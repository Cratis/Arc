// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The binding table of <see cref="ArcOptions"/>, in declaration order.
/// </summary>
internal static class ArcOptionsBinding
{
    /// <summary>
    /// Gets how <see cref="ArcOptions"/> is bound.
    /// </summary>
    internal static readonly ObjectType Bound = new(typeof(ArcOptions), static () => new ArcOptions(), Members);

    static Member[] Members() =>
    [
        Member.Of<ArcOptions, JsonSerializerOptions>("JsonSerializerOptions", JsonSerializerOptionsBinding.Bound, static _ => _.JsonSerializerOptions),
        Member.Of<ArcOptions, Execution.CorrelationIdOptions>("CorrelationId", CorrelationIdOptionsBinding.Bound, static _ => _.CorrelationId, static (o, v) => o.CorrelationId = v),
        Member.Of<ArcOptions, Tenancy.TenancyOptions>("Tenancy", TenancyOptionsBinding.Bound, static _ => _.Tenancy, static (o, v) => o.Tenancy = v),
        Member.Of<ArcOptions, Connections.ConnectionResolutionOptions>("ConnectionResolution", ConnectionResolutionOptionsBinding.Bound, static _ => _.ConnectionResolution, static (o, v) => o.ConnectionResolution = v),
        Member.Of<ArcOptions, Type?>("IdentityDetailsProvider", new AbstractType(typeof(Type)), static _ => _.IdentityDetailsProvider, static (o, v) => o.IdentityDetailsProvider = v),
        Member.Of<ArcOptions, ApiEndpointOptions>("GeneratedApis", ApiEndpointOptionsBinding.Bound, static _ => _.GeneratedApis, static (o, v) => o.GeneratedApis = v),
        Member.Of<ArcOptions, Introspection.IntrospectionOptions>("Introspection", IntrospectionOptionsBinding.Bound, static _ => _.Introspection, static (o, v) => o.Introspection = v),
        Member.Of<ArcOptions, bool>("TrustForwardedIdentityHeaders", Leaves.Bool, static _ => _.TrustForwardedIdentityHeaders, static (o, v) => o.TrustForwardedIdentityHeaders = v),
        Member.Of<ArcOptions, Identity.OnBehalfOfOptions>("OnBehalfOf", OnBehalfOfOptionsBinding.Bound, static _ => _.OnBehalfOf, static (o, v) => o.OnBehalfOf = v),
        Member.Of<ArcOptions, Identity.ForwardedIdentityOptions>("ForwardedIdentity", ForwardedIdentityOptionsBinding.Bound, static _ => _.ForwardedIdentity, static (o, v) => o.ForwardedIdentity = v),
        Member.Of<ArcOptions, Calendar.CalendarOptions>("Calendar", CalendarOptionsBinding.Bound, static _ => _.Calendar, static (o, v) => o.Calendar = v),
        Member.Of<ArcOptions, Commands.CommandOptions>("Command", CommandOptionsBinding.Bound, static _ => _.Command, static (o, v) => o.Command = v),
        Member.Of<ArcOptions, Queries.QueryOptions>("Query", QueryOptionsBinding.Bound, static _ => _.Query, static (o, v) => o.Query = v),
        Member.Of<ArcOptions, Queries.QueryHealthOptions>("QueryHealth", QueryHealthOptionsBinding.Bound, static _ => _.QueryHealth, static (o, v) => o.QueryHealth = v),
        Member.Of<ArcOptions, HostingOptions>("Hosting", HostingOptionsBinding.Bound, static _ => _.Hosting, static (o, v) => o.Hosting = v),
        Member.Of<ArcOptions, EndpointGroups.EndpointGroupsOptions>("EndpointGroups", EndpointGroupsOptionsBinding.Bound, static _ => _.EndpointGroups, static (o, v) => o.EndpointGroups = v),
        Member.Of<ArcOptions, bool>("ExposeExceptionDetails", Leaves.Bool, static _ => _.ExposeExceptionDetails, static (o, v) => o.ExposeExceptionDetails = v)
    ];
}
