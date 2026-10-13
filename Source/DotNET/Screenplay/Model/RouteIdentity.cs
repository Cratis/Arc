// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Identifies a route in both the model and the runtime stored-name spaces.
/// </summary>
/// <param name="Source">The model source name.</param>
/// <param name="Stream">The model stream name.</param>
/// <param name="StoredSource">The effective stored source name.</param>
/// <param name="StoredStream">The effective stored stream name.</param>
public record RouteIdentity(string Source, string? Stream, string StoredSource, string? StoredStream)
{
    /// <summary>
    /// Checks whether another route names the same event source, in both name spaces, regardless of stream.
    /// </summary>
    /// <param name="other">The route to compare with.</param>
    /// <returns><see langword="true"/> when both the model and stored source names match.</returns>
    public bool HasSameSourceAs(RouteIdentity other) => SameModelSource(other) && SameStoredSource(other);

    bool SameModelSource(RouteIdentity other) => Source == other.Source;

    bool SameStoredSource(RouteIdentity other) => StoredSource == other.StoredSource;
}
