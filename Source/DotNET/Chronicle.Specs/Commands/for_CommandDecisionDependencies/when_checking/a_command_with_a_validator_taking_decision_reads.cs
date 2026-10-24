// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;
using Cratis.Arc.Chronicle.Testing.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandDecisionDependencies.when_checking;

public class a_command_with_a_validator_taking_decision_reads : Specification
{
    bool _result;

    void Because() => _result = CommandDecisionDependencies.Declares(typeof(TokenCommand));

    [Fact] void should_declare_a_decision_dependency() => _result.ShouldBeTrue();
}
