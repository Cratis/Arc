// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_constructing_a_validator_under_on : given.protected_plain_models
{
    Validator _validator;
    object? _handle;

    void Because()
    {
        var types = Substitute.For<Cratis.Types.ITypes>();
        types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(Validator)]);
        var validators = new DiscoverableValidators(types);
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(Transfer));
        CommandTransaction.Current = _unit;
        using var frame = _unit.EnforceReadProtection();
        CommandDecisionReads.Begin(typeof(Transfer));
        try
        {
            validators.TryGet(typeof(Transfer), _services, out var validator).ShouldBeTrue();
            _validator = (Validator)validator;
            _handle = Resolve(HandleParameters[0]);
        }
        finally
        {
            CommandDecisionReads.End();
        }
    }

    [Fact] void should_construct_the_real_discoverable_validator() => _validator.ShouldNotBeNull();
    [Fact] void should_share_the_guarded_instance_with_handle() => _validator.Balance.ShouldBeSame(_handle);
    [Fact] void should_fold_once() => _tokens.Count.ShouldEqual(1);
    [Fact] void should_never_read_the_sink() => _stored.DidNotReceiveWithAnyArgs().GetInstanceById(default!, default!);
}
