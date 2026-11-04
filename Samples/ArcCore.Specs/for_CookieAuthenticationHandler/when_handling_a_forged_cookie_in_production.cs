// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Samples.ArcCore.Specs.for_CookieAuthenticationHandler;

public class when_handling_a_forged_cookie_in_production : given.a_handler_in_environment
{
    async Task Because() => _result = await Handle("Production");

    [Fact] void should_not_authenticate() => _result.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_not_fail_so_other_handlers_can_run() => _result.Failure.ShouldBeNull();
}
