// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Carries an actual runtime source definition without inferred keys.
/// </summary>
/// <param name="Identity">The definition's qualified type identity.</param>
/// <param name="Source">Its exact stored source name.</param>
/// <param name="Streams">Its exact declared stream names.</param>
internal record ReactorSourceDefinition(string Identity, string Source, IReadOnlyList<string> Streams)
{
    /// <summary>
    /// Gets the exact stored source name.
    /// </summary>
    internal string StoredSource { get; init; } = Source;

    /// <summary>
    /// Gets stored stream names keyed by their recovered model names.
    /// </summary>
    internal IReadOnlyDictionary<string, string> StoredStreams { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);
}
