// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using static Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_using_decision_mode;

namespace Cratis.Arc.Chronicle.Commands.for_CommandDecisionDependencies.when_checking;

public class a_command_reading_a_decision_read : Specification
{
    bool _result;

    void Because() => _result = CommandDecisionDependencies.Declares(typeof(DecideAtSource));

    [Fact] void should_declare_a_decision_dependency() => _result.ShouldBeTrue();
}
