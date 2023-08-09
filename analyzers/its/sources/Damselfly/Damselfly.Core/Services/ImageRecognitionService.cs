﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;
using Damselfly.Core.Utils.ML;
using Damselfly.ML.Face.Azure;
using Damselfly.ML.Face.Emgu;
using Damselfly.ML.ImageClassification;
using Damselfly.ML.ObjectDetection;
using Damselfly.Shared.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp.PixelFormats;

namespace Damselfly.Core.Services;

public class ImageRecognitionService : IPeopleService, IProcessJobFactory, IRescanProvider
{
    private readonly AzureFaceService _azureFaceService;
    private readonly ConfigService _configService;
    private readonly EmguFaceService _emguFaceService;
    private readonly ExifService _exifService;
    private readonly ImageCache _imageCache;
    private readonly ImageClassifier _imageClassifier;
    private readonly ImageProcessService _imageProcessor;
    private readonly MetaDataService _metdataService;
    private readonly ObjectDetector _objectDetector;

    // WASM: This should be a MemoryCache
    private readonly IDictionary<string, Person> _peopleCache = new ConcurrentDictionary<string, Person>();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IStatusService _statusService;
    private readonly ThumbnailService _thumbService;
    private readonly WorkService _workService;

    public ImageRecognitionService(IServiceScopeFactory scopeFactory,
        IStatusService statusService, ObjectDetector objectDetector,
        MetaDataService metadataService, AzureFaceService azureFace,
        EmguFaceService emguService,
        ThumbnailService thumbs, ConfigService configService,
        ImageClassifier imageClassifier, ImageCache imageCache,
        WorkService workService, ExifService exifService,
        ImageProcessService imageProcessor)
    {
        _scopeFactory = scopeFactory;
        _thumbService = thumbs;
        _azureFaceService = azureFace;
        _statusService = statusService;
        _objectDetector = objectDetector;
        _metdataService = metadataService;
        _emguFaceService = emguService;
        _configService = configService;
        _imageClassifier = imageClassifier;
        _imageProcessor = imageProcessor;
        _imageCache = imageCache;
        _workService = workService;
        _exifService = exifService;
    }

    public ImageRecognitionService()
    {
    }

    public static bool EnableImageRecognition { get; set; } = true;

    public async Task<List<Person>> GetAllPeople()
    {
        await LoadPersonCache();

        return _peopleCache.Values.OrderBy(x => x?.Name).ToList();
    }

    public async Task<Person> GetPerson( int personId )
    {
        await LoadPersonCache();

        return _peopleCache.Values.FirstOrDefault(x => x.PersonId == personId);
    }

    public async Task<List<string>> GetPeopleNames(string searchText)
    {
        await LoadPersonCache();

        // Union the search term with the results from the DB. Order them so that the
        // closest matches to the start of the string come first.
        var names = _peopleCache.Values
            .Where(x => x.Name.StartsWith(searchText.Trim(), StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Name)
            .Distinct()
            .OrderBy(x => x.ToUpper().IndexOf(searchText.ToUpper()))
            .ThenBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return names;
    }

    public async Task UpdateName(ImageObject faceObject, string name)
    {
        if (!faceObject.IsFace)
            throw new ArgumentException("Image object passed to name update.");

        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        if (faceObject.Person == null)
        {
            faceObject.Person = new Person();
            db.People.Add(faceObject.Person);
        }
        else
        {
            db.People.Update(faceObject.Person);
        }

        // TODO: If this is an existing person/name, we might need to merge in Azure
        faceObject.Person.Name = name;
        faceObject.Person.State = Person.PersonState.Identified;
        faceObject.Person.LastUpdated = DateTime.UtcNow;
        db.ImageObjects.Update(faceObject);

        await db.SaveChangesAsync("SetName");

        if (faceObject.Person.AzurePersonId != null)
        {
            // Add/update the cache
            _peopleCache[faceObject.Person.AzurePersonId] = faceObject.Person;
        }

        _imageCache.Evict(faceObject.ImageId);
    }

    public async Task UpdatePerson(Person person, string name)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        // TODO: If this is an existing person/name, we might need to merge in Azure
        person.Name = name;
        person.State = Person.PersonState.Identified;
        person.LastUpdated = DateTime.UtcNow;
        db.People.Update(person);

        await db.SaveChangesAsync("SetName");

        // Add/update the cache
        _peopleCache[person.AzurePersonId] = person;
    }

    public JobPriorities Priority => JobPriorities.ImageRecognition;

    public async Task<ICollection<IProcessJob>> GetPendingJobs(int maxJobs)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        // Only pull out images where the thumb *has* been processed, and the
        // metadata has already been scanned, the AI hasn't been processed.
        var images = await db.ImageMetaData.Where(x => x.LastUpdated >= x.Image.LastUpdated
                                                       && x.ThumbLastUpdated != null
                                                       && x.AILastUpdated == null)
            .OrderByDescending(x => x.LastUpdated)
            .Take(maxJobs)
            .Select(x => x.ImageId)
            .ToListAsync();

        if ( images.Any() )
        {
            var jobs = images.Select(x => new AIProcess { ImageId = x, Service = this })
                .ToArray();
            return jobs;
        }

        return new AIProcess[0];
    }

    public async Task MarkFolderForScan(int folderId)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        //var queryable = db.Set<ImageMetaData>().Where(img => img.Image.FolderId == folder.FolderId);
        //int updated = await db.BatchUpdate(queryable, x => new ImageMetaData { AILastUpdated = null });

        var updated = await ImageContext.UpdateMetadataFields(db, folderId, "AILastUpdated", "null");

        if ( updated != 0 )
            _statusService.UpdateStatus($"{updated} images in folder flagged for AI reprocessing.");

        _workService.FlagNewJobs(this);
    }

    public async Task MarkAllForScan()
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        var updated = await db.BatchUpdate(db.ImageMetaData, i => i.SetProperty(x => x.AILastUpdated, x => null));

        _statusService.UpdateStatus($"All {updated} images flagged for AI reprocessing.");

        _workService.FlagNewJobs(this);
    }

    public async Task MarkImagesForScan(ICollection<int> images)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        var queryable = db.ImageMetaData.Where(i => images.Contains(i.ImageId));

        var rows = await db.BatchUpdate(queryable, i => i.SetProperty(x => x.AILastUpdated, x => null));

        var msgText = rows == 1 ? "Image" : $"{rows} images";
        _statusService.UpdateStatus($"{msgText} flagged for AI reprocessing.");
    }

    private int GetPersonIDFromCache(Guid? azurePersonId)
    {
        if ( azurePersonId.HasValue )
        {
            // TODO Await
            LoadPersonCache().Wait();

            if ( _peopleCache.TryGetValue(azurePersonId.ToString(), out var person) )
                return person.PersonId;
        }

        return 0;
    }

    /// <summary>
    ///     Initialise the in-memory cache of people.
    /// </summary>
    /// <param name="force"></param>
    private async Task LoadPersonCache(bool force = false)
    {
        try
        {
            if ( force || !_peopleCache.Any() )
            {
                _peopleCache.Clear();

                var watch = new Stopwatch("LoadPersonCache");

                using var scope = _scopeFactory.CreateScope();
                using var db = scope.ServiceProvider.GetService<ImageContext>();

                var dict = await db.People.Where(x => !string.IsNullOrEmpty(x.AzurePersonId))
                    .AsNoTracking()
                    .Select(p => new { p.AzurePersonId, Person = p })
                    .ToListAsync();

                if ( dict.Any() )
                {
                    // Merge the items into the people cache. Note that we use
                    // the indexer to avoid dupe key issues. TODO: Should the table be unique?
                    dict.ToList().ForEach(x => _peopleCache[x.AzurePersonId] = x.Person);

                    Logging.LogTrace("Pre-loaded cach with {0} people.", _peopleCache.Count());
                }

                watch.Stop();
            }
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Unexpected exception loading people cache: {ex.Message}");
        }
    }

    /// <summary>
    ///     Create the DB entries for people who we don't know about,
    ///     and then pre-populate the cache with their entries.
    /// </summary>
    /// <param name="personIdsToAdd"></param>
    /// <returns></returns>
    public async Task CreateMissingPeople(IEnumerable<string> personIdsToAdd)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        try
        {
            if ( personIdsToAdd != null && personIdsToAdd.Any() )
            {
                // Find the people that aren't already in the cache and add new ones
                // Be careful - filter out empty ones (shouldn't ever happen, but belt
                // and braces
                var newNames = personIdsToAdd.Select(x => x.Trim())
                    .Where(x => !string.IsNullOrEmpty(x) && !_peopleCache.ContainsKey(x))
                    .ToList();

                if ( newNames.Any() )
                {
                    Logging.Log($"Adding {newNames.Count()} person records.");

                    var newPeople = newNames.Select(x => new Person
                    {
                        AzurePersonId = x,
                        Name = "Unknown",
                        State = Person.PersonState.Unknown,
                        LastUpdated = DateTime.UtcNow
                    }).ToList();

                    if ( newPeople.Any() )
                    {
                        await db.BulkInsert(db.People, newPeople);

                        // Add or replace the new people in the cache (this should always add)
                        newPeople.ForEach(x => _peopleCache[x.AzurePersonId] = x);
                    }
                }
            }
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Exception in CreateMissingPeople: {ex.Message}");
        }
    }

    /// <summary>
    ///     Given a collection of detected objects, create the tags, put them in the cache,
    ///     and then return a list of keyword => TagID key-value pairs
    /// </summary>
    /// <param name="objects"></param>
    /// <returns></returns>
    private async Task<IDictionary<string, int>> CreateNewTags(IList<ImageDetectResult> objects)
    {
        var allLabels = objects.Select(x => x.Tag).Distinct().ToList();
        var tags = await _metdataService.CreateTagsFromStrings(allLabels);

        return tags.ToDictionary(x => x.Keyword, y => y.TagId, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Returns true if ImagesWithFaces is selected and any of the
    ///     objects contains either a person or a face
    /// </summary>
    /// <param name="objects"></param>
    /// <returns></returns>
    private bool UseAzureForRecogition(IList<ImageDetectResult> objects)
    {
        if ( _azureFaceService.DetectionType == AzureDetection.ImagesWithFaces )
            if ( objects.Any(x => string.Compare(x.Tag, "face", true) == 0 ||
                                  string.Compare(x.Tag, "person", true) == 0) )
                return true;

        return false;
    }

    /// <summary>
    ///     Detect objects in the image.
    /// </summary>
    /// <param name="image"></param>
    /// <returns></returns>
    private async Task DetectObjects(ImageMetaData metadata)
    {
        var image = metadata.Image;
        var fileName = new FileInfo(image.FullPath);

        if ( !fileName.Exists )
            return;

        try
        {
            var thumbSize = ThumbSize.Large;
            var medThumb = new FileInfo(_thumbService.GetThumbPath(fileName, thumbSize));
            var enableAIProcessing = _configService.GetBool(ConfigSettings.EnableAIProcessing, true);

            MetaDataService.GetImageSize(medThumb.FullName, out var thumbWidth, out var thumbHeight);

            var foundObjects = new List<ImageObject>();
            var foundFaces = new List<ImageObject>();

            if ( enableAIProcessing || _azureFaceService.DetectionType == AzureDetection.AllImages )
                Logging.Log($"Processing AI image detection for {fileName.Name}...");

            if ( !File.Exists(medThumb.FullName) )
                // The thumb isn't ready yet. 
                return;

            using var theImage = SixLabors.ImageSharp.Image.Load<Rgb24>(medThumb.FullName);

            if ( _imageClassifier != null && enableAIProcessing )
            {
                var colorWatch = new Stopwatch("DetectObjects");

                var dominant = _imageClassifier.DetectDominantColour(theImage);
                var average = _imageClassifier.DetectAverageColor(theImage);

                colorWatch.Stop();

                image.MetaData.AverageColor = average.ToHex();
                image.MetaData.DominantColor = dominant.ToHex();

                Logging.LogVerbose(
                    $"Image {image.FullPath} has dominant colour {dominant.ToHex()}, average {average.ToHex()}");
            }

            // Next, look for faces. We need to determine if we:
            //  a) Use only local (EMGU) detection
            //  b) Use local detection, and then if we find a face, or a person object, submit to Azure
            //  c) Always submit every image to Azure.
            // This is a user config.
            var useAzureDetection = false;

            // For the object detector, we need a successfully loaded bitmap
            if ( enableAIProcessing )
            {
                var objwatch = new Stopwatch("DetectObjects");

                // First, look for Objects
                var objects = await _objectDetector.DetectObjects(theImage);

                objwatch.Stop();

                if ( objects.Any() )
                {
                    Logging.Log($" Yolo found {objects.Count()} objects in {fileName}...");

                    var newTags = await CreateNewTags(objects);

                    var newObjects = objects.Select(x => new ImageObject
                    {
                        RecogntionSource = ImageObject.RecognitionType.MLNetObject,
                        ImageId = image.ImageId,
                        RectX = x.Rect.Left,
                        RectY = x.Rect.Top,
                        RectHeight = x.Rect.Height,
                        RectWidth = x.Rect.Width,
                        TagId = x.IsFace ? 0 : newTags[x.Tag],
                        Type = ImageObject.ObjectTypes.Object.ToString(),
                        Score = x.Score
                    }).ToList();

                    if ( UseAzureForRecogition(objects) )
                        useAzureDetection = true;

                    ScaleObjectRects(image, newObjects, thumbWidth, thumbHeight);
                    foundObjects.AddRange(newObjects);
                }
            }

            if ( _azureFaceService.DetectionType == AzureDetection.AllImages )
                // Skip local face detection and just go straight to Azure
                useAzureDetection = true;
            else if ( enableAIProcessing )
                if ( _emguFaceService.ServiceAvailable )
                {
                    var emguwatch = new Stopwatch("EmguFaceDetect");

                    var rects = _emguFaceService.DetectFaces(medThumb.FullName);

                    emguwatch.Stop();

                    if ( UseAzureForRecogition(rects) )
                    {
                        // Filter out the faces if we're using Azure
                        rects = rects.Where(x => !x.IsFace).ToList();
                        useAzureDetection = true;
                    }

                    if ( rects.Any() )
                    {
                        // Azure is disabled, so just use what we've got.
                        Logging.Log($" Emgu found {rects.Count} faces in {fileName}...");

                        var newTags = await CreateNewTags(rects);

                        var newObjects = rects.Select(x => new ImageObject
                        {
                            RecogntionSource = ImageObject.RecognitionType.Emgu,
                            ImageId = image.ImageId,
                            RectX = x.Rect.Left,
                            RectY = x.Rect.Top,
                            RectHeight = x.Rect.Height,
                            RectWidth = x.Rect.Width,
                            TagId = newTags[x.Tag],
                            Type = x.IsFace
                                ? ImageObject.ObjectTypes.Face.ToString()
                                : ImageObject.ObjectTypes.Object.ToString(),
                            Score = 0
                        }).ToList();

                        ScaleObjectRects(image, newObjects, thumbWidth, thumbHeight);
                        foundFaces.AddRange(newObjects);
                    }
                }

            if ( useAzureDetection )
            {
                var faceTag = await _metdataService.CreateTagsFromStrings(new List<string> { "Face" });
                var faceTagId = faceTag.FirstOrDefault()?.TagId ?? 0;

                var azurewatch = new Stopwatch("AzureFaceDetect");

                Logging.LogVerbose($"Processing {medThumb.FullName} with Azure Face Service");

                // We got predictions or we're scanning everything - so now let's try the image with Azure.
                var azureFaces = await _azureFaceService.DetectFaces(medThumb.FullName, _imageProcessor);

                azurewatch.Stop();

                if ( azureFaces.Any() )
                {
                    Logging.Log($" Azure found {azureFaces.Count} faces in {fileName}...");

                    // Get a list of the Azure Person IDs
                    var peopleIds = azureFaces.Select(x => x.PersonId.ToString());

                    // Create any new ones, or pull existing ones back from the cache
                    await CreateMissingPeople(peopleIds);

                    // Now convert into ImageObjects. Note that if the peopleCache doesn't
                    // contain the key, it means we didn't create a person record successfully
                    // for that entry - so we skip it.
                    var newObjects = azureFaces.Select(x => new ImageObject
                    {
                        ImageId = image.ImageId,
                        RectX = x.Left,
                        RectY = x.Top,
                        RectHeight = x.Height,
                        RectWidth = x.Width,
                        Type = ImageObject.ObjectTypes.Face.ToString(),
                        TagId = faceTagId,
                        RecogntionSource = ImageObject.RecognitionType.Azure,
                        Score = x.Score,
                        PersonId = GetPersonIDFromCache(x.PersonId)
                    }).ToList();

                    ScaleObjectRects(image, newObjects, thumbWidth, thumbHeight);
                    foundFaces.AddRange(newObjects);

                    var peopleToAdd = foundFaces.Select(x => x.Person);

                    // Add them
                }
                else
                {
                    // If we're scanning because local face detection found a face, log the result.
                    if ( _azureFaceService.DetectionType == AzureDetection.ImagesWithFaces )
                        Logging.Log($"Azure found no faces in image {fileName}");
                    else
                        Logging.LogVerbose($"Azure found no faces in image {fileName}");
                }
            }

            if ( foundFaces.Any() )
            {
                // We've found some faces. Add a tagID.
                const string faceTagName = "Face";
                var tags = await _metdataService.CreateTagsFromStrings(new List<string> { faceTagName });
                var faceTagId = tags.Single().TagId;
                foundFaces.ForEach(x => x.TagId = faceTagId);
            }

            if ( foundObjects.Any() || foundFaces.Any() )
            {
                var objWriteWatch = new Stopwatch("WriteDetectedObjects");

                var allFound = foundObjects.Union(foundFaces).ToList();

                using var scope = _scopeFactory.CreateScope();
                using var db = scope.ServiceProvider.GetService<ImageContext>();

                // First, clear out the existing faces and objects - we don't want dupes
                // TODO: Might need to be smarter about this once we add face names and
                // Object identification details.
                await db.BatchDelete(db.ImageObjects.Where(x =>
                    x.ImageId.Equals(image.ImageId) && x.RecogntionSource != ImageObject.RecognitionType.ExternalApp));
                // Now add the objects and faces.
                await db.BulkInsert(db.ImageObjects, allFound);

                WriteAITagsToImages(image, allFound);

                objWriteWatch.Stop();
            }
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Exception during AI detection for {fileName}: {ex}");
        }
    }

    /// <summary>
    ///     Write the tags to the image
    /// </summary>
    /// <param name="tags"></param>
    private void WriteAITagsToImages(Image image, List<ImageObject> tags)
    {
        if ( _configService.GetBool(ConfigSettings.WriteAITagsToImages) )
        {
            Logging.Log("Writing AI tags to image Metadata...");

            // Seleect the tag IDs that aren't faces.
            var tagIdsToAdd = tags.Where(x => !x.IsFace)
                .Select(x => x.TagId)
                .Distinct()
                .ToList();

            if ( tagIdsToAdd.Any() )
            {
                // Get their keywords
                var keywordsToAdd = _metdataService.CachedTags
                    .Where(x => tagIdsToAdd.Contains(x.TagId))
                    .Select(x => x.Keyword)
                    .ToList();

                if ( keywordsToAdd.Any() )
                    // Fire and forget this asynchronously - we don't care about waiting for it
                    _ = _exifService.UpdateTagsAsync(image, keywordsToAdd);
            }

            // Seleect the tag IDs that are faces.
            var facesToAdd = tags.Where(x => x.IsFace)
                .Distinct()
                .ToList();

            if ( facesToAdd.Any() )
                // Fire and forget this asynchronously - we don't care about waiting for it
                _ = _exifService.UpdateFaceDataAsync(new[] { image }, facesToAdd);
        }
    }

    /// <summary>
    ///     Scales the detected face/object rectangles based on the full-sized image,
    ///     since the object detection was done on a smaller thumbnail.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="imgObjects">Collection of objects to scale</param>
    /// <param name="thumbSize"></param>
    private void ScaleObjectRects(Image image, List<ImageObject> imgObjects, int bmpWidth, int bmpHeight)
    {
        if ( bmpHeight == 0 || bmpWidth == 0 )
            return;

        float longestBmpSide = bmpWidth > bmpHeight ? bmpWidth : bmpHeight;
        float longestImgSide =
            image.MetaData.Width > image.MetaData.Height ? image.MetaData.Width : image.MetaData.Height;
        var ratio = longestImgSide / longestBmpSide;

        foreach ( var imgObj in imgObjects )
        {
            imgObj.RectX = (int)(imgObj.RectX * ratio);
            imgObj.RectY = (int)(imgObj.RectY * ratio);
            imgObj.RectWidth = (int)(imgObj.RectWidth * ratio);
            imgObj.RectHeight = (int)(imgObj.RectHeight * ratio);
        }

        ;
    }

    public void StartService()
    {
        if ( !EnableImageRecognition )
        {
            Logging.Log("AI Image recognition service was disabled.");
            return;
        }

        _ = LoadPersonCache();

        _workService.AddJobSource(this);
    }

    /// <summary>
    ///     Work processing method for AI processing for a single
    ///     Image.
    /// </summary>
    /// <param name="imageId"></param>
    /// <returns></returns>
    private async Task DetectObjects(int imageId)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        var image = await _imageCache.GetCachedImage(imageId);

        db.Attach(image);

        // First, update the timestamp. We do this first, so that even if something
        // fails, it'll be set, avoiding infinite loops of failed processing.
        // The caller will update the DB with a SaveChanges call.
        image.MetaData.AILastUpdated = DateTime.UtcNow;

        try
        {
            await DetectObjects(image.MetaData);
        }
        finally
        {
            // The DetectObjects method will set the metadata AILastUpdated
            // timestamp. It may also update other fields.
            db.ImageMetaData.Update(image.MetaData);
            await db.SaveChangesAsync("UpdateAIGenDate");
        }

        _imageCache.Evict(imageId);
    }

    public class AIProcess : IProcessJob
    {
        public int ImageId { get; set; }
        public ImageRecognitionService Service { get; set; }
        public string Name => "AI processing";
        public string Description => $"{Name} for ID: {ImageId}";
        public JobPriorities Priority => JobPriorities.ImageRecognition;

        public async Task Process()
        {
            await Service.DetectObjects(ImageId);
        }

        public bool CanProcess => true;

        public override string ToString()
        {
            return Description;
        }
    }
}
