// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Samples.ArcCore.Specs.for_PublishedArcCore;

public class when_authenticating_with_a_forged_cookie_in_production : given.an_authenticated_query_request
{
    protected override string HostEnvironment => "Production";

    async Task Because() => await GetAuthenticatedWithForgedCookie();

    [PublishedArcCoreFact] void should_refuse_the_caller() => _statusCode.ShouldEqual(HttpStatusCode.Unauthorized);
}
