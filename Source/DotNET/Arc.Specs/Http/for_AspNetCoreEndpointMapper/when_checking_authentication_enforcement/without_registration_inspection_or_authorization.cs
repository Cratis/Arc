// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_AspNetCoreEndpointMapper.when_checking_authentication_enforcement;

public class without_registration_inspection_or_authorization : given.a_host
{
    void Establish() => Build(supportsRegistrationInspection: false);

    void Because() => _problem = _guard.FindEnforcementProblem(null);

    [Fact] void should_report_missing_authorization() => _problem.ShouldEqual("Requiring authentication on the discovery endpoints needs ASP.NET Core authorization services (AddAuthorization).");
}
