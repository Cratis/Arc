// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Samples.ArcCore.Specs.Testing;

/// <summary>
/// The exception that is thrown when no published ArcCore executable is configured.
/// </summary>
public class PublishedArcCoreNotConfigured() : Exception(
    "Set CRATIS_ARC_PUBLISHED_ARCCORE to the absolute path of a published ArcCore executable. " +
    "See Samples/README.md for the three dotnet publish commands using " +
    "CratisSamplePublish=Untrimmed, CratisSamplePublish=Trimmed and CratisSamplePublish=NativeAot.");
