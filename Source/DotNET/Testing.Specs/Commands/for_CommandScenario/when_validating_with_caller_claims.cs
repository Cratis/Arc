// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.for_CommandScenario;

public class when_validating_with_caller_claims : Specification
{
    readonly CommandScenario<RunAdministration> _scenario = new();
    CommandResult _result = default!;

    void Establish() => _scenario.WithClaims(new Claim(ClaimTypes.Role, "Administrator"));

    async Task Because() => _result = await _scenario.Validate(new RunAdministration());

    [Fact] void should_authorize_using_the_claims() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_validate_successfully() => _result.IsSuccess.ShouldBeTrue();

    void Destroy() => _scenario.Dispose();
}
