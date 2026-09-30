// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;

public class when_a_registered_validator_captures_a_decision_reader : Specification
{
    Exception _exception;

    void Because()
    {
        using var policy = DecisionPolicyForSpecs.Begin(typeof(ProtectedCommand));
        _exception = Record.Exception(() => new DecisionDependencySafety().ValidateRegisteredValidator(typeof(UnsafeValidator)));
    }

    [Fact] void should_fail_closed_instead_of_capturing_a_scoped_reader() => _exception.ShouldBeOfExactType<RegisteredValidatorRefusedInProtectedDecision>();

    [ProtectedDecision]
    public class ProtectedCommand;

    public class UnsafeValidator(IDecisionReads reads)
    {
        public IDecisionReads Reads { get; } = reads;
    }
}
