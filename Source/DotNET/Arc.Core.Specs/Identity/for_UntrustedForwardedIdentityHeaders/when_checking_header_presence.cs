// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_UntrustedForwardedIdentityHeaders;

public class when_checking_header_presence
{
    [Fact] void should_detect_the_principal_header() => UntrustedForwardedIdentityHeaders.ArePresent(header => header == MicrosoftIdentityPlatformHeaders.PrincipalHeader).ShouldBeTrue();
    [Fact] void should_detect_the_id_header() => UntrustedForwardedIdentityHeaders.ArePresent(header => header == MicrosoftIdentityPlatformHeaders.IdentityIdHeader).ShouldBeTrue();
    [Fact] void should_detect_the_name_header() => UntrustedForwardedIdentityHeaders.ArePresent(header => header == MicrosoftIdentityPlatformHeaders.IdentityNameHeader).ShouldBeTrue();
    [Fact] void should_not_detect_unrelated_headers() => UntrustedForwardedIdentityHeaders.ArePresent(header => header == "Authorization").ShouldBeFalse();
    [Fact] void should_not_detect_an_empty_request() => UntrustedForwardedIdentityHeaders.ArePresent(_ => false).ShouldBeFalse();
}
