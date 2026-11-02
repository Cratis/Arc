// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Validation.for_ValidatorShouldExtensions.when_asserting_a_rejection;

public class and_only_a_substring_matches : Specification
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => new given.an_item_validator().ShouldReject(new given.AddItem(""), "required"));
    [Fact] void should_require_an_exact_message() => _error.ShouldBeOfExactType<ValidatorAssertionException>();
}
