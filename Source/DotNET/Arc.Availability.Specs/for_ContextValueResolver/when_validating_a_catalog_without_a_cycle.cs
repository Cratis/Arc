// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;
using Cratis.Arc.Availability.Context.Providers;

namespace Cratis.Arc.Availability.for_ContextValueResolver;

public class when_validating_a_catalog_without_a_cycle : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() =>
        new ContextValueCatalog()
            .Declare(new ContextValueDeclaration("a", new ExpressionContextValueProvider("b"), ContextScope.Application))
            .Declare(new ContextValueDeclaration("b", new LiteralContextValueProvider(1), ContextScope.Application))
            .Validate());

    [Fact] void should_not_throw() => _error.ShouldBeNull();
}
