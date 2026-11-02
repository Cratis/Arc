// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Validation.for_ValidatorShouldExtensions.when_asserting_acceptance;

public class and_a_rule_applies_only_to_commands : Specification
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => new given.an_item_validator().ShouldAccept(new given.AddItem("command-only")));
    [Fact] void should_run_command_rules() => _error.ShouldBeOfExactType<ValidatorAssertionException>();
    [Fact] void should_show_the_command_rule_message() => _error.Message.ShouldContain("Command name is reserved");
}
