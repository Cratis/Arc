// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation.for_DiscoverableValidators;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation.for_ProtectedValidatorConstruction;

public class when_a_transient_validator_is_resolved_outside_a_protected_decision : Specification
{
    ServiceProvider _provider;
    object _resolved = null!;

    void Establish()
    {
        var services = new ServiceCollection().AddTransient<CommandWithoutDependenciesValidator>();
        services.ConstructDependencyFreeValidatorsThroughArc(Cratis.Types.Types.Instance);
        _provider = services.BuildServiceProvider();
    }

    void Because() => _resolved = _provider.GetRequiredService<CommandWithoutDependenciesValidator>();

    void Destroy() => _provider.Dispose();

    [Fact] void should_construct_the_validator() => _resolved.ShouldBeOfExactType<CommandWithoutDependenciesValidator>();
    [Fact] void should_not_spend_a_certificate_on_an_instance_no_protected_decision_can_reuse() => ProtectedValidatorConstruction.IsCertified(_resolved, typeof(CommandWithoutDependenciesValidator)).ShouldBeFalse();
}
