// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_UntrustedForwardedIdentityHeadersStartupWarning;

public class when_starting_with_on_behalf_of_configured : given.a_startup_warning
{
    void Establish()
    {
        _options.TrustForwardedIdentityHeaders = true;
        _options.OnBehalfOf = OnBehalfOfAssertionsForSpecs.Options();
    }

    Task Because() => _warning.StartAsync(CancellationToken.None);

    [Fact] void should_not_log_anything() => _messages.ShouldBeEmpty();
}
