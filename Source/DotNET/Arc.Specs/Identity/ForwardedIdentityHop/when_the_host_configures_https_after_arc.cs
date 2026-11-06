// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Authentication;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace Cratis.Arc.Identity.for_ForwardedIdentityHop;

public class when_the_host_configures_https_after_arc : given.host_https_defaults
{
    void Because() => Configure(arcFirst: true);

    [Fact] void should_preserve_certificate_selection() => Https.ServerCertificateSelector.ShouldEqual(Selection);
    [Fact] void should_preserve_the_hosts_protocol_policy() => Https.SslProtocols.ShouldEqual(SslProtocols.None);
    [Fact] void should_preserve_required_client_certificates() => Https.ClientCertificateMode.ShouldEqual(ClientCertificateMode.RequireCertificate);
    [Fact] void should_preserve_the_hosts_validation_callback() => Https.ClientCertificateValidation.ShouldEqual(Validation);
}
