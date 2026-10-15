// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_reusing_a_read_in_a_nested_on_command : given.protected_plain_models
{
    object? _outer;
    object? _nested;

    void Because()
    {
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(Transfer));
        CommandTransaction.Current = _unit;
        using var frame = _unit.EnforceReadProtection();
        CommandDecisionReads.Begin(typeof(Transfer));
        try
        {
            _outer = Resolve(HandleParameters[0]);
            using var child = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(Transfer));
            CommandDecisionReads.Begin(typeof(Transfer));
            try
            {
                _nested = Resolve(HandleParameters[0]);
            }
            finally
            {
                CommandDecisionReads.End();
            }
        }
        finally
        {
            CommandDecisionReads.End();
        }
    }

    [Fact] void should_keep_the_earliest_read_in_the_shared_unit() => _tokens.Count.ShouldEqual(1);
    [Fact] void should_share_the_released_instance() => _nested.ShouldBeSame(_outer);
    [Fact] void should_release_only_once() => _stored.Received(1).Release(_from);
}
