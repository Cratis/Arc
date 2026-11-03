// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The binding table of <see cref="TemplateConnectionResolverOptions"/>.
/// </summary>
internal static class TemplateConnectionResolverOptionsBinding
{
    /// <summary>
    /// Gets how <see cref="TemplateConnectionResolverOptions"/> is bound.
    /// </summary>
    internal static readonly ObjectType Bound = new(typeof(TemplateConnectionResolverOptions), static () => new TemplateConnectionResolverOptions(), Members);

    static Member[] Members() =>
    [
        Member.Of<TemplateConnectionResolverOptions, string?>("ConnectionString", Leaves.String, static _ => _.ConnectionString, static (o, v) => o.ConnectionString = v),
        Member.Of<TemplateConnectionResolverOptions, string?>("Database", Leaves.String, static _ => _.Database, static (o, v) => o.Database = v),
        Member.Of<TemplateConnectionResolverOptions, string?>("Schema", Leaves.String, static _ => _.Schema, static (o, v) => o.Schema = v)
    ];
}
