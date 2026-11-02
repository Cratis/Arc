// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The binding table of <see cref="HttpConnectionResolverOptions"/>.
/// </summary>
internal static class HttpConnectionResolverOptionsBinding
{
    /// <summary>
    /// Gets how <see cref="HttpConnectionResolverOptions"/> is bound.
    /// </summary>
    internal static readonly ObjectType Bound = new(typeof(HttpConnectionResolverOptions), static () => new HttpConnectionResolverOptions(), Members);

    static Member[] Members() =>
    [
        Member.Of<HttpConnectionResolverOptions, Uri?>("Endpoint", Leaves.Uri, static _ => _.Endpoint, static (o, v) => o.Endpoint = v),
        Member.Of<HttpConnectionResolverOptions, IDictionary<string, string>>("Headers", new DictionaryType<string>(Leaves.String), static _ => _.Headers, static (o, v) => o.Headers = v),
        Member.Of<HttpConnectionResolverOptions, TimeSpan>("Timeout", Leaves.TimeSpan, static _ => _.Timeout, static (o, v) => o.Timeout = v),
        Member.Of<HttpConnectionResolverOptions, HttpConnectionResolverAuthenticationOptions>("Authentication", HttpConnectionResolverAuthenticationOptionsBinding.Bound, static _ => _.Authentication, static (o, v) => o.Authentication = v)
    ];
}
