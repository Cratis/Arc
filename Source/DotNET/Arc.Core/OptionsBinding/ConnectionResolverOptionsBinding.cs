// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The binding table of <see cref="ConnectionResolverOptions"/>.
/// </summary>
internal static class ConnectionResolverOptionsBinding
{
    /// <summary>
    /// Gets how <see cref="ConnectionResolverOptions"/> is bound.
    /// </summary>
    internal static readonly ObjectType Bound = new(typeof(ConnectionResolverOptions), static () => new ConnectionResolverOptions(), Members);

    static Member[] Members() =>
    [
        Member.Of<ConnectionResolverOptions, ConnectionResolverType>("Resolver", Leaves.ConnectionResolverType, static _ => _.Resolver, static (o, v) => o.Resolver = v),
        Member.Of<ConnectionResolverOptions, TemplateConnectionResolverOptions>("Template", TemplateConnectionResolverOptionsBinding.Bound, static _ => _.Template, static (o, v) => o.Template = v),
        Member.Of<ConnectionResolverOptions, HttpConnectionResolverOptions>("Http", HttpConnectionResolverOptionsBinding.Bound, static _ => _.Http, static (o, v) => o.Http = v),

        // CustomType carries a trimming annotation that an unannotated setter value could not satisfy. An abstract member is only
        // ever written back with the value its getter returned, so the setter writes that value again.
        Member.Of<ConnectionResolverOptions, Type?>("CustomType", new AbstractType(typeof(Type)), static _ => _.CustomType, static (o, _) => o.CustomType = o.CustomType)
    ];
}
