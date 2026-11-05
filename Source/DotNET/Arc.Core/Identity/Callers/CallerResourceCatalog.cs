// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;

namespace Cratis.Arc.Identity.Callers;

/// <summary>An exact, ordinal resource URI to receiving audience mapping; distinct identifiers are not equated or normalized.</summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
/// <param name="resources">The explicit resource-to-audience mapping.</param>
internal sealed class CallerResourceCatalog(IReadOnlyDictionary<string, string> resources)
{
    readonly FrozenDictionary<string, string> _resources = resources.ToFrozenDictionary(StringComparer.Ordinal);
    readonly bool _oneToOne = resources.Values.Distinct(StringComparer.Ordinal).Count() == resources.Count;

    /// <summary>Looks up an exact resource URI without normalization.</summary>
    /// <param name="resource">The receiving resource.</param>
    /// <param name="audience">The mapped audience.</param>
    /// <returns>Whether the resource is registered.</returns>
    internal bool TryGetAudience(string resource, out string audience)
    {
        audience = string.Empty;
        return _oneToOne && _resources.TryGetValue(resource, out audience!);
    }
}
