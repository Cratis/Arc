// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(ChangeSetComputorMetadataUpdateHandler))]

namespace Cratis.Arc.Queries;

/// <summary>
/// Clears <see cref="ChangeSetComputor"/>'s per-type caches when Hot Reload updates types.
/// </summary>
internal static class ChangeSetComputorMetadataUpdateHandler
{
    /// <summary>
    /// Called by the runtime after a metadata update.
    /// </summary>
    /// <param name="updatedTypes">The types that were updated, or <see langword="null"/> when unknown.</param>
    public static void ClearCache(Type[]? updatedTypes)
    {
        // Every cached type may be affected, since an update can add an identity property or an Equals override.
        _ = updatedTypes;
        ChangeSetComputor.ClearCaches();
    }
}
