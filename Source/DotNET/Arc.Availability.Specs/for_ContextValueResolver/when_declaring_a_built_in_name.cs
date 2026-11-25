// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;
using Cratis.Arc.Availability.Context.Providers;

namespace Cratis.Arc.Availability.for_ContextValueResolver;

public class when_declaring_a_built_in_name : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() =>
        new ContextValueCatalog().Declare(new ContextValueDeclaration("tenant", new LiteralContextValueProvider(1), ContextScope.Of("Fleet"))));

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<ReservedContextValueNameException>();
}
