// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using static Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_using_decision_mode;

namespace Cratis.Arc.Chronicle.Commands.for_CommandDecisionDependencies.when_checking;

public class the_same_command_twice : Specification
{
    bool _first;
    bool _second;

    void Establish() => _first = CommandDecisionDependencies.Declares(typeof(DecideAtSource));

    void Because() => _second = CommandDecisionDependencies.Declares(typeof(DecideAtSource));

    [Fact] void should_find_the_dependency_again() => _second.ShouldBeTrue();
    [Fact] void should_preserve_the_answer() => _second.ShouldEqual(_first);
}
