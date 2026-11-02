// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Monads;

namespace Cratis.Arc.Testing.Commands.for_CommandRejectionShouldExtensions.when_asserting_acceptance;

public class and_it_was_accepted : Specification
{
    Result<string, ValidationResult> _result = "accepted";
    string _value;
    void Because() => _value = _result.ShouldBeAccepted();
    [Fact] void should_return_the_success_value() => _value.ShouldEqual("accepted");
}
