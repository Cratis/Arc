// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity.for_UntrustedForwardedIdentityHeadersStartupWarning;

public class when_starting_with_signed_forwarded_identity : given.a_startup_warning
{
    void Establish() => _options.ForwardedIdentity.Enabled = true;

    Task Because() => _warning.StartAsync(CancellationToken.None);

    [Fact] void should_not_warn_that_every_request_is_anonymous() => _messages.Count(message => message.Level == LogLevel.Warning).ShouldEqual(0);
    [Fact] void should_leave_the_header_trust_default_unchanged() => _options.TrustForwardedIdentityHeaders.ShouldBeFalse();
}
