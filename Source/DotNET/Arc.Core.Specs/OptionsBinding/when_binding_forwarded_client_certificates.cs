// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.OptionsBinding;

public class when_binding_forwarded_client_certificates : given_a_parity_run
{
    protected override IDictionary<string, string?> Configuration => new Dictionary<string, string?>
    {
        ["Cratis:Arc:ForwardedIdentity:ClientCertificate:Required"] = "true",
        ["Cratis:Arc:ForwardedIdentity:ClientCertificate:TrustedCertificateAuthoritiesPath"] = "/certificates/ca.pem",
        ["Cratis:Arc:ForwardedIdentity:ClientCertificate:AllowedDnsNames:0"] = "authproxy.internal",
        ["Cratis:Arc:ForwardedIdentity:ClientCertificate:ReloadInterval"] = "00:00:30"
    };
}
