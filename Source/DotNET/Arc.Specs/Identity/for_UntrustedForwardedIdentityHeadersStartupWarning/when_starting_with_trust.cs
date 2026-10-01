// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_UntrustedForwardedIdentityHeadersStartupWarning;

public class when_starting_with_trust : given.a_startup_warning
{
    void Establish() => _options.TrustForwardedIdentityHeaders = true;

    Task Because() => _warning.StartAsync(CancellationToken.None);

    [Fact] void should_not_log_a_warning() => _messages.ShouldBeEmpty();
}
