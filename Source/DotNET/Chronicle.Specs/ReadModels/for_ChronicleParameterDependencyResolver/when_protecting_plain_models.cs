// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_protecting_plain_models : given.protected_plain_models
{
    object? _validator;
    object? _handle;
    object? _provide;
    object? _other;
    bool _enrolledBeforeExposure;

    void Because()
    {
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(Transfer));
        CommandTransaction.Current = _unit;
        using var frame = _unit.EnforceReadProtection();
        CommandDecisionReads.Begin(typeof(Transfer));
        try
        {
            _validator = Resolve(typeof(Validator).GetConstructors()[0].GetParameters()[0]);
            _enrolledBeforeExposure = _unit.HasEnrolledDecisionReads;
            _handle = Resolve(HandleParameters[0]);
            _provide = Resolve(typeof(Transfer).GetMethod(nameof(Transfer.Provide))!.GetParameters()[0]);
            _other = Resolve(HandleParameters[1]);
        }
        finally
        {
            CommandDecisionReads.End();
        }
    }

    [Fact] void should_enroll_before_exposing_the_validator_dependency() => _enrolledBeforeExposure.ShouldBeTrue();
    [Fact] void should_share_the_released_validator_and_handle_instance() => _handle.ShouldBeSame(_validator);
    [Fact] void should_share_the_provide_instance() => _provide.ShouldBeSame(_validator);
    [Fact] void should_honor_the_second_explicit_lookup_key() => _other.ShouldBeSame(_to);
    [Fact] void should_acquire_one_fresh_fold_per_key() => _tokens.Count.ShouldEqual(2);
    [Fact] void should_never_read_the_sink() => _stored.DidNotReceiveWithAnyArgs().GetInstanceById(default!, default!);
    [Fact] void should_release_the_shared_instance_only_once() => _stored.Received(1).Release(_from);
    [Fact] void should_never_acquire_a_detached_read() => _inner.DidNotReceiveWithAnyArgs().GetDetached<Balance>(default!);
}
