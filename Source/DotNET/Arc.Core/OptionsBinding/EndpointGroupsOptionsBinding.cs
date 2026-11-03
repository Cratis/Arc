// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EndpointGroups;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The binding table of <see cref="EndpointGroupsOptions"/>.
/// </summary>
internal static class EndpointGroupsOptionsBinding
{
    /// <summary>
    /// Gets how <see cref="EndpointGroupsOptions"/> is bound.
    /// </summary>
    internal static readonly ObjectType Bound = new(typeof(EndpointGroupsOptions), static () => new EndpointGroupsOptions(), Members);

    static Member[] Members() =>
    [
        Member.Of<EndpointGroupsOptions, IDictionary<string, IList<int>>>("Ports", new DictionaryType<IList<int>>(new ListType<int>(Leaves.Int)), static _ => _.Ports, static (o, v) => o.Ports = v),
        Member.Of<EndpointGroupsOptions, IDictionary<string, string>>("RoutePrefixes", new DictionaryType<string>(Leaves.String), static _ => _.RoutePrefixes, static (o, v) => o.RoutePrefixes = v),
        Member.Of<EndpointGroupsOptions, bool>("ServeDefaultGroupOnGroupPorts", Leaves.Bool, static _ => _.ServeDefaultGroupOnGroupPorts, static (o, v) => o.ServeDefaultGroupOnGroupPorts = v),
        Member.Of<EndpointGroupsOptions, bool>("HasGroups", Leaves.Bool, static _ => _.HasGroups)
    ];
}
