// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_CommandScenario;

public class when_a_principal_override_is_registered : Specification
{
    readonly CommandScenario<InspectPrincipalOverride> _scenario = new();
    ICurrentPrincipalOverride _principalOverride = default!;
    CommandResult _result = default!;

    void Establish()
    {
        _principalOverride = Substitute.For<ICurrentPrincipalOverride>();
        _scenario.Services.AddSingleton(_principalOverride);
    }

    async Task Because() => _result = await _scenario.Execute(new InspectPrincipalOverride(_principalOverride));

    [Fact] void should_execute_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_resolve_the_registered_instance() => ((CommandResult<bool>)_result).Response.ShouldBeTrue();

    void Destroy() => _scenario.Dispose();
}
