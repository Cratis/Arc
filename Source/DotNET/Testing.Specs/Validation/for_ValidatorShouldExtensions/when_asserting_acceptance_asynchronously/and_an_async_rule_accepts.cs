// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Validation.for_ValidatorShouldExtensions.when_asserting_acceptance_asynchronously;

public class and_an_async_rule_accepts : Specification
{
    Exception _error;
    async Task Because() => _error = await Catch.Exception(() => new given.an_asynchronous_item_validator().ShouldAcceptAsync(new given.AddItem("item")));
    [Fact] void should_pass() => _error.ShouldBeNull();
}
