// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Monads;

namespace Cratis.Arc.Testing.Commands.for_CommandRejectionShouldExtensions.when_asserting_a_rejection;

public class and_it_was_rejected : Specification
{
    ValidationResult _rejection = ValidationResult.Warning("Unavailable");
    ValidationResult _matched;
    void Because()
    {
        Result<string, ValidationResult> result = _rejection;
        _matched = result.ShouldBeRejected();
    }
    [Fact] void should_return_the_rejection_regardless_of_severity() => _matched.ShouldEqual(_rejection);
}
