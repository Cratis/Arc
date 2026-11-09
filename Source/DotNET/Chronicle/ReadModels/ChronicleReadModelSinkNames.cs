// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Sinks;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Sinks;
using Cratis.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Represents an <see cref="IReadModelSinkNameProvider"/> that gives the named sink Chronicle stores a read model in.
/// </summary>
/// <remarks>
/// Follows the Chronicle client: a read model's <see cref="SinkAttribute"/> names its sink, else the client's default sink
/// name applies to the read models the client registers. Passive read models have no sink. Types the client does not know as
/// read models keep the default storage unless they declare a sink themselves.
/// </remarks>
/// <param name="readModelTypes">The read model types the Chronicle client registers.</param>
/// <param name="defaultSinkName">The client's default sink name; empty when the Kernel chooses.</param>
[IgnoreConvention]
public class ChronicleReadModelSinkNames(IEnumerable<Type> readModelTypes, string? defaultSinkName) : IReadModelSinkNameProvider
{
    readonly HashSet<Type> _readModelTypes = [.. readModelTypes];

    /// <inheritdoc/>
    public string? GetSinkNameFor(Type readModelType)
    {
        if (readModelType.IsPassive())
        {
            return null;
        }

        return readModelType.GetSinkName() ??
            (_readModelTypes.Contains(readModelType) && !string.IsNullOrWhiteSpace(defaultSinkName) ? defaultSinkName.Trim() : null);
    }
}
