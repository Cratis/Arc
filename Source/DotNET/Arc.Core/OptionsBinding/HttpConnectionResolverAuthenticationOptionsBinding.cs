// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The binding table of <see cref="HttpConnectionResolverAuthenticationOptions"/>.
/// </summary>
internal static class HttpConnectionResolverAuthenticationOptionsBinding
{
    /// <summary>
    /// Gets how <see cref="HttpConnectionResolverAuthenticationOptions"/> is bound.
    /// </summary>
    internal static readonly ObjectType Bound = new(typeof(HttpConnectionResolverAuthenticationOptions), static () => new HttpConnectionResolverAuthenticationOptions(), Members);

    static Member[] Members() =>
    [
        Member.Of<HttpConnectionResolverAuthenticationOptions, HttpConnectionResolverAuthenticationType>("Type", Leaves.HttpConnectionResolverAuthenticationType, static _ => _.Type, static (o, v) => o.Type = v),
        Member.Of<HttpConnectionResolverAuthenticationOptions, string?>("Scope", Leaves.String, static _ => _.Scope, static (o, v) => o.Scope = v),
        Member.Of<HttpConnectionResolverAuthenticationOptions, Uri?>("TokenEndpoint", Leaves.Uri, static _ => _.TokenEndpoint, static (o, v) => o.TokenEndpoint = v),
        Member.Of<HttpConnectionResolverAuthenticationOptions, string?>("ClientId", Leaves.String, static _ => _.ClientId, static (o, v) => o.ClientId = v),
        Member.Of<HttpConnectionResolverAuthenticationOptions, string?>("ClientSecret", Leaves.String, static _ => _.ClientSecret, static (o, v) => o.ClientSecret = v),
        Member.Of<HttpConnectionResolverAuthenticationOptions, AzureCredentialType>("Credential", Leaves.AzureCredentialType, static _ => _.Credential, static (o, v) => o.Credential = v),
        Member.Of<HttpConnectionResolverAuthenticationOptions, TimeSpan>("RefreshMargin", Leaves.TimeSpan, static _ => _.RefreshMargin, static (o, v) => o.RefreshMargin = v)
    ];
}
