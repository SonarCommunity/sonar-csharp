﻿'------------------------------------------------------------------------------
' <auto-generated>
'     This code was generated by a tool.
'     Runtime Version:4.0.30319.42000
'
'     Changes to this file may cause incorrect behavior and will be lost if
'     the code is regenerated.
' </auto-generated>
'------------------------------------------------------------------------------

Option Strict On
Option Explicit On

Imports System

Namespace My.Resources
    
    'This class was auto-generated by the StronglyTypedResourceBuilder
    'class via a tool like ResGen or Visual Studio.
    'To add or remove a member, edit your .ResX file then rerun ResGen
    'with the /str option, or rebuild your VS project.
    '''<summary>
    '''  A strongly-typed resource class, for looking up localized strings, etc.
    '''</summary>
    <Global.System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0"),  _
     Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
     Global.System.Runtime.CompilerServices.CompilerGeneratedAttribute(),  _
     Global.Microsoft.VisualBasic.HideModuleNameAttribute()>  _
    Friend Module Resources
        
        Private resourceMan As Global.System.Resources.ResourceManager
        
        Private resourceCulture As Global.System.Globalization.CultureInfo
        
        '''<summary>
        '''  Returns the cached ResourceManager instance used by this class.
        '''</summary>
        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)>  _
        Friend ReadOnly Property ResourceManager() As Global.System.Resources.ResourceManager
            Get
                If Object.ReferenceEquals(resourceMan, Nothing) Then
                    Dim temp As Global.System.Resources.ResourceManager = New Global.System.Resources.ResourceManager("EmberAPI.Resources", GetType(Resources).Assembly)
                    resourceMan = temp
                End If
                Return resourceMan
            End Get
        End Property
        
        '''<summary>
        '''  Overrides the current thread's CurrentUICulture property for all
        '''  resource lookups using this strongly typed resource class.
        '''</summary>
        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)>  _
        Friend Property Culture() As Global.System.Globalization.CultureInfo
            Get
                Return resourceCulture
            End Get
            Set
                resourceCulture = value
            End Set
        End Property
        
        '''<summary>
        '''  Looks up a localized resource of type System.Drawing.Bitmap.
        '''</summary>
        Friend ReadOnly Property defaultgenre() As System.Drawing.Bitmap
            Get
                Dim obj As Object = ResourceManager.GetObject("defaultgenre", resourceCulture)
                Return CType(obj,System.Drawing.Bitmap)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized resource of type System.Drawing.Bitmap.
        '''</summary>
        Friend ReadOnly Property defaultscreen() As System.Drawing.Bitmap
            Get
                Dim obj As Object = ResourceManager.GetObject("defaultscreen", resourceCulture)
                Return CType(obj,System.Drawing.Bitmap)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized resource of type System.Drawing.Bitmap.
        '''</summary>
        Friend ReadOnly Property defaultsound() As System.Drawing.Bitmap
            Get
                Dim obj As Object = ResourceManager.GetObject("defaultsound", resourceCulture)
                Return CType(obj,System.Drawing.Bitmap)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized resource of type System.Drawing.Bitmap.
        '''</summary>
        Friend ReadOnly Property haslanguage() As System.Drawing.Bitmap
            Get
                Dim obj As Object = ResourceManager.GetObject("haslanguage", resourceCulture)
                Return CType(obj,System.Drawing.Bitmap)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to CREATE TABLE IF NOT EXISTS Jobs
        '''(
        '''	ID INTEGER PRIMARY KEY AUTOINCREMENT,
        '''	MediaType INTEGER NOT NULL,
        '''	MediaID INTEGER NOT NULL,
        '''	LastDateAdd TEXT
        ''');
        '''
        '''CREATE TABLE IF NOT EXISTS JobsEntry
        '''(
        '''	ID INTEGER PRIMARY KEY AUTOINCREMENT,
        '''	ItemType INTEGER NOT NULL,
        '''	Message INTEGER NOT NULL,
        '''	Details INTEGER NOT NULL,
        '''	DateAdd TEXT
        ''');.
        '''</summary>
        Friend ReadOnly Property JobsDatabaseSQL_v1() As String
            Get
                Return ResourceManager.GetString("JobsDatabaseSQL_v1", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to CREATE TABLE Sets(
        '''	SetName TEXT NOT NULL PRIMARY KEY
        '''	);
        '''
        '''CREATE TABLE Movies (
        '''	ID INTEGER PRIMARY KEY,
        '''	MoviePath TEXT,
        '''	Type BOOL,
        '''	ListTitle TEXT,
        '''	HasPoster BOOL,
        '''	HasFanart BOOL,
        '''	HasNfo BOOL,
        '''	HasTrailer BOOL,
        '''	HasSub BOOL,
        '''	HasExtra BOOL,
        '''	New BOOL,
        '''	Mark BOOL,
        '''	Source TEXT,
        '''	Imdb TEXT,
        '''	Lock BOOL,
        '''	Title TEXT,
        '''	OriginalTitle TEXT,
        '''	Year TEXT,
        '''	Rating TEXT,
        '''	Votes TEXT,
        '''	MPAA TEXT,
        '''	Top250 TEXT,
        '''	Country TEXT,
        '''	Outline TEXT,
        '''	Plot TEXT,
        '''	Tagline TEXT,
        '''	Certification T [rest of string was truncated]&quot;;.
        '''</summary>
        Friend ReadOnly Property MediaDatabaseSQL_v1() As String
            Get
                Return ResourceManager.GetString("MediaDatabaseSQL_v1", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized resource of type System.Drawing.Bitmap.
        '''</summary>
        Friend ReadOnly Property missing() As System.Drawing.Bitmap
            Get
                Dim obj As Object = ResourceManager.GetObject("missing", resourceCulture)
                Return CType(obj,System.Drawing.Bitmap)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized resource of type System.Drawing.Bitmap.
        '''</summary>
        Friend ReadOnly Property overlay() As System.Drawing.Bitmap
            Get
                Dim obj As Object = ResourceManager.GetObject("overlay", resourceCulture)
                Return CType(obj,System.Drawing.Bitmap)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized resource of type System.Drawing.Bitmap.
        '''</summary>
        Friend ReadOnly Property overlay2() As System.Drawing.Bitmap
            Get
                Dim obj As Object = ResourceManager.GetObject("overlay2", resourceCulture)
                Return CType(obj,System.Drawing.Bitmap)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized resource of type System.Drawing.Bitmap.
        '''</summary>
        Friend ReadOnly Property puzzle() As System.Drawing.Bitmap
            Get
                Dim obj As Object = ResourceManager.GetObject("puzzle", resourceCulture)
                Return CType(obj,System.Drawing.Bitmap)
            End Get
        End Property
    End Module
End Namespace
