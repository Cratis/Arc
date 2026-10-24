// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using static Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_automatically_enabling_decision_reads;

namespace Cratis.Arc.Chronicle.Commands.for_CommandDecisionDependencies.when_checking;

public class a_plain_command : Specification
{
    bool _result;

    void Because() => _result = CommandDecisionDependencies.Declares(typeof(Ordinary));

    [Fact] void should_not_declare_a_decision_dependency() => _result.ShouldBeFalse();
}
