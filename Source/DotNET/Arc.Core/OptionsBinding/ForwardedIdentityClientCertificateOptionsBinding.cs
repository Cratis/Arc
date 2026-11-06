// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;

namespace Cratis.Arc.OptionsBinding;

/// <summary>The reflection-free client-certificate options binding.</summary>
internal static class ForwardedIdentityClientCertificateOptionsBinding
{
    /// <summary>Gets the client-certificate options binding.</summary>
    internal static readonly ObjectType Bound = new(typeof(ForwardedIdentityClientCertificateOptions), static () => new ForwardedIdentityClientCertificateOptions(), Members);

    static Member[] Members() =>
    [
        Member.Of<ForwardedIdentityClientCertificateOptions, bool>("Required", Leaves.Bool, static _ => _.Required, static (o, v) => o.Required = v),
        Member.Of<ForwardedIdentityClientCertificateOptions, string?>("TrustedCertificateAuthoritiesPath", Leaves.String, static _ => _.TrustedCertificateAuthoritiesPath, static (o, v) => o.TrustedCertificateAuthoritiesPath = v),
        Member.Of<ForwardedIdentityClientCertificateOptions, IList<string>>("AllowedDnsNames", new ListType<string>(Leaves.String), static _ => _.AllowedDnsNames, static (o, v) => o.AllowedDnsNames = v),
        Member.Of<ForwardedIdentityClientCertificateOptions, TimeSpan>("ReloadInterval", Leaves.TimeSpan, static _ => _.ReloadInterval, static (o, v) => o.ReloadInterval = v)
    ];
}
