// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Validation.for_ValidatorShouldExtensions.when_asserting_a_rejection;

public class and_the_failure_is_a_warning : Specification
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => new given.an_item_validator().ShouldReject(new given.AddItem("item", 0), "Quantity must be positive"));
    [Fact] void should_accept_a_warning_as_a_rejection() => _error.ShouldBeNull();
}
