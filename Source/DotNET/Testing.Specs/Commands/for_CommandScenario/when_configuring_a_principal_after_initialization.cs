// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.for_CommandScenario;

public class when_configuring_a_principal_after_initialization : Specification
{
    readonly CommandScenario<RunAdministration> _scenario = new();
    Exception? _exception;

    async Task Establish() => await _scenario.Validate(new RunAdministration());

    void Because() => _exception = Catch.Exception(() => _scenario.WithPrincipal(new ClaimsPrincipal()));

    [Fact] void should_reject_the_configuration() => _exception.ShouldBeOfExactType<ScenarioPrincipalMustBeConfiguredBeforeInitialization>();

    void Destroy() => _scenario.Dispose();
}
