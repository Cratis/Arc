// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Validation.for_ValidatorShouldExtensions.when_asserting_acceptance;

public class and_the_command_is_null : Specification
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => new given.an_item_validator().ShouldAccept(null!));
    [Fact] void should_refuse_null() => _error.ShouldBeOfExactType<ArgumentNullException>();
}
