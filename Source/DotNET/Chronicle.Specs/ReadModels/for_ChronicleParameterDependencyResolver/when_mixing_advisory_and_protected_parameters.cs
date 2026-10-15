// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_mixing_advisory_and_protected_parameters : given.protected_plain_models
{
    object? _advisory;
    object? _protected;
    bool _advisoryEnrolled;

    void Because()
    {
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(Transfer));
        CommandTransaction.Current = _unit;
        using var frame = _unit.EnforceReadProtection();
        CommandDecisionReads.Begin(typeof(Transfer));
        try
        {
            var parameters = typeof(Transfer).GetMethod(nameof(Transfer.Enrich))!.GetParameters();
            _advisory = Resolve(parameters[0]);
            _advisoryEnrolled = _unit.HasEnrolledDecisionReads;
            _protected = Resolve(parameters[1]);
        }
        finally
        {
            CommandDecisionReads.End();
        }
    }

    [Fact] void should_read_advisory_context_from_storage() => _advisory.ShouldBeSame(_from);
    [Fact] void should_not_enroll_the_advisory_read() => _advisoryEnrolled.ShouldBeFalse();
    [Fact] void should_acquire_a_new_fold_for_the_protected_parameter() => _tokens.Count.ShouldEqual(1);
    [Fact] void should_expose_the_protected_instance() => _protected.ShouldBeSame(_from);
    [Fact] void should_enroll_the_protected_read() => _unit.HasEnrolledDecisionReads.ShouldBeTrue();
}
