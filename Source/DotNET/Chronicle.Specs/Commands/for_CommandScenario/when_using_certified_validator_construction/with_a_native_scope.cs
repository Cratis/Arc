// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_using_certified_validator_construction;

public class with_a_native_scope : given.a_certified_command
{
    CommandResult _validation;
    CommandResult _execution;
    readonly List<CertifiedValidator> _seen = [];

    void Establish() => _scenario.Services.AddProtectedDecisionValidator<CertifiedValidator>(_seen.Add);

    async Task Because()
    {
        Build();
        _validation = await _pipeline.Validate(_command, _scope.ServiceProvider);
        _execution = await _pipeline.Execute(_command, _scope.ServiceProvider);
    }

    [Fact]
    void should_retain_the_unselected_framework_open_token_binding()
    {
        ProtectedDecisionProviderAttestations.TryGetAttestation(_scope.ServiceProvider, out var attestation).ShouldBeTrue();
        attestation.Root.Manifest.BindingsFor(typeof(DecisionRead<when_using_decision_mode.DecisionState>))
            .Any(binding => binding.ServiceType == typeof(DecisionRead<>) && attestation.Root.Manifest.OriginOf(binding) is { IsConvention: true }).ShouldBeTrue();
    }

    [Fact] void should_validate_successfully() => _validation.ShouldBeSuccessful();
    [Fact] void should_execute_successfully() => _execution.ShouldBeSuccessful();
    [Fact] void should_append_only_for_execution() => _scenario.AppendedEvents.Count.ShouldEqual(1);
    [Fact] void should_construct_a_fresh_validator_for_validate_and_execute() => _seen.Count.ShouldEqual(2);
    [Fact] void should_issue_separate_tokens_for_the_two_invocations() => _seen[0].Read.ShouldNotBeSame(_seen[1].Read);
    [Fact] void should_enroll_absent_state_against_the_command_key() => _seen.TrueForAll(validator => !validator.Read.Exists).ShouldBeTrue();
}
