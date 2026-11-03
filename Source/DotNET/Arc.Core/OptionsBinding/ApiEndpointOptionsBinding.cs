// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The binding table of <see cref="ApiEndpointOptions"/>.
/// </summary>
internal static class ApiEndpointOptionsBinding
{
    /// <summary>
    /// Gets how <see cref="ApiEndpointOptions"/> is bound.
    /// </summary>
    internal static readonly ObjectType Bound = new(typeof(ApiEndpointOptions), static () => new ApiEndpointOptions(), Members);

    static Member[] Members() =>
    [
        Member.Of<ApiEndpointOptions, string>("RoutePrefix", Leaves.String, static _ => _.RoutePrefix, static (o, v) => o.RoutePrefix = v),
        Member.Of<ApiEndpointOptions, int>("SegmentsToSkipForRoute", Leaves.Int, static _ => _.SegmentsToSkipForRoute, static (o, v) => o.SegmentsToSkipForRoute = v),
        Member.Of<ApiEndpointOptions, bool>("IncludeCommandNameInRoute", Leaves.Bool, static _ => _.IncludeCommandNameInRoute, static (o, v) => o.IncludeCommandNameInRoute = v),
        Member.Of<ApiEndpointOptions, bool>("IncludeQueryNameInRoute", Leaves.Bool, static _ => _.IncludeQueryNameInRoute, static (o, v) => o.IncludeQueryNameInRoute = v),
        Member.Of<ApiEndpointOptions, bool>("EnableQueryHttpMethod", Leaves.Bool, static _ => _.EnableQueryHttpMethod, static (o, v) => o.EnableQueryHttpMethod = v)
    ];
}
