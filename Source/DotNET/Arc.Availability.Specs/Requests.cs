// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;
using Cratis.Arc.Availability.Context;

namespace Cratis.Arc.Availability;

/// <summary>
/// Builds request contexts for specifications.
/// </summary>
public static class Requests
{
    public static EffectiveAudiences Holding(params string[] audiences) =>
        new(new HashSet<AudienceName>(audiences.Select(_ => new AudienceName(_))), []);

    public static ContextValueResolver For(
        EffectiveAudiences? audiences,
        ContextValueCatalog? catalog = null,
        IReadOnlyDictionary<string, object?>? ambient = null,
        ContextScope? scope = null) =>
        (catalog ?? new ContextValueCatalog()).CreateResolver(
            scope ?? ContextScope.Application,
            ambient,
            audiences is null ? null : _ => ValueTask.FromResult<EffectiveAudiences?>(audiences));
}
