// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity.for_UntrustedForwardedIdentityHeadersStartupWarning;

public class when_starting_with_misconfigured_on_behalf_of : given.a_startup_warning
{
    void Establish()
    {
        _options.TrustForwardedIdentityHeaders = true;
        _options.OnBehalfOf = new() { Enabled = true };
    }

    Task Because() => _warning.StartAsync(CancellationToken.None);

    [Fact] void should_log_an_error() => _messages.Single().Level.ShouldEqual(LogLevel.Error);
    [Fact] void should_say_what_is_missing() => _messages.Single().Message.ShouldContain("an issuer and an audience are required");
}
