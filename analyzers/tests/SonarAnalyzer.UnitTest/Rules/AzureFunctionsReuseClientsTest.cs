﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2022 SonarSource SA
 * mailto: contact AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using SonarAnalyzer.Rules.CSharp;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class AzureFunctionsReuseClientsTest
    {
        private readonly VerifierBuilder builder = new VerifierBuilder<AzureFunctionsReuseClients>()
            .WithBasePath("CloudNative")
            .AddReferences(NuGetMetadataReference.MicrosoftNetSdkFunctions())
            .AddReferences(MetadataReferenceFacade.SystemThreadingTasks)
            .AddReferences(NuGetMetadataReference.SystemNetHttp())
            .AddReferences(NuGetMetadataReference.MicrosoftExtensionsHttp());

        [TestMethod]
        public void AzureFunctionsReuseClients_HttpClient_CS() =>
            builder.AddPaths("AzureFunctionsReuseClients_HttpClient.cs").Verify();

        [TestMethod]
        public void AzureFunctionsReuseClients_HttpClient_CS9() =>
            builder.AddPaths("AzureFunctionsReuseClients_HttpClient.CSharp9.cs").WithOptions(ParseOptionsHelper.FromCSharp9).Verify();

        [TestMethod]
        public void AzureFunctionsReuseClients_DocumentClient_CS() =>
            builder.AddReferences(NuGetMetadataReference.MicrosoftAzureDocumentDB())
                   .AddPaths("AzureFunctionsReuseClients_DocumentClient.cs")
                   .Verify();

        [TestMethod]
        public void AzureFunctionsReuseClients_CosmosClient_CS() =>
            builder.AddReferences(NuGetMetadataReference.MicrosoftAzureCosmos())
                   .AddPaths("AzureFunctionsReuseClients_CosmosClient.cs")
                   .Verify();

        [TestMethod]
        public void AzureFunctionsReuseClients_ServiceBusV5_CS() =>
            builder.AddReferences(NuGetMetadataReference.MicrosoftAzureServiceBus())
                   .AddPaths("AzureFunctionsReuseClients_ServiceBusV5.cs")
                   .Verify();

        [TestMethod]
        public void AzureFunctionsReuseClients_ServiceBusV7_CS() =>
            builder.AddReferences(NuGetMetadataReference.AzureMessagingServiceBus())
                    .AddPaths("AzureFunctionsReuseClients_ServiceBusV7.cs")
                    .Verify();

        [TestMethod]
        public void AzureFunctionsReuseClients_Storage_CS() =>
            builder.AddReferences(NuGetMetadataReference.AzureCore())
                   .AddReferences(NuGetMetadataReference.AzureStorageCommon())
                   .AddReferences(NuGetMetadataReference.AzureStorageBlobs())
                   .AddReferences(NuGetMetadataReference.AzureStorageQueues())
                   .AddReferences(NuGetMetadataReference.AzureStorageFilesShares())
                   .AddReferences(NuGetMetadataReference.AzureStorageFilesDataLake())
                   .AddPaths("AzureFunctionsReuseClients_Storage.cs")
                   .Verify();

        [TestMethod]
        public void AzureFunctionsReuseClients_ArmClient_CS() =>
            builder.AddReferences(NuGetMetadataReference.AzureCore())
                   .AddReferences(NuGetMetadataReference.AzureIdentity())
                   .AddReferences(NuGetMetadataReference.AzureResourceManager())
                   .AddPaths("AzureFunctionsReuseClients_ArmClient.cs")
                   .Verify();
    }
}
