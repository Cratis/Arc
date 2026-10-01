// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity.for_UntrustedForwardedIdentityHeadersStartupWarning;

public class when_starting_without_trust : given.a_startup_warning
{
    Task Because() => _warning.StartAsync(CancellationToken.None);

    [Fact] void should_log_one_warning() => _messages.Count(message => message.Level == LogLevel.Warning).ShouldEqual(1);
    [Fact] void should_name_the_opt_in() => _messages.Single().Message.ShouldContain("TrustForwardedIdentityHeaders");
}
