// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;
using Cratis.Arc.Availability.Context.Providers;

namespace Cratis.Arc.Availability.for_ContextValueResolver;

public class when_validating_a_catalog_with_a_cycle : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() =>
        new ContextValueCatalog()
            .Declare(new ContextValueDeclaration("a", new ExpressionContextValueProvider("$context.b.x"), ContextScope.Application))
            .Declare(new ContextValueDeclaration("b", new ExpressionContextValueProvider("a"), ContextScope.Application))
            .Validate());

    [Fact] void should_detect_the_cycle_before_a_request_runs() => _error.ShouldBeOfExactType<ContextValueCycleException>();
}
