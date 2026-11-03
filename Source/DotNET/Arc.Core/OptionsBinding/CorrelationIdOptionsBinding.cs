// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Execution;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The binding table of <see cref="CorrelationIdOptions"/>.
/// </summary>
internal static class CorrelationIdOptionsBinding
{
    /// <summary>
    /// Gets how <see cref="CorrelationIdOptions"/> is bound.
    /// </summary>
    internal static readonly ObjectType Bound = new(typeof(CorrelationIdOptions), static () => new CorrelationIdOptions(), Members);

    static Member[] Members() =>
    [
        Member.Of<CorrelationIdOptions, string>("HttpHeader", Leaves.String, static _ => _.HttpHeader, static (o, v) => o.HttpHeader = v)
    ];
}
