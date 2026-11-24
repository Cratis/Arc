// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;
using Cratis.Arc.Availability.Context.Providers;

namespace Cratis.Arc.Availability.for_ContextValueResolver;

public class when_a_lower_level_overrides_a_value : Specification
{
    ContextValueOutcome _slice;
    ContextValueOutcome _sibling;
    ContextValueOutcome _application;
    ContextValueDeclaration _winner;

    async Task Because()
    {
        var catalog = new ContextValueCatalog()
            .Declare(new ContextValueDeclaration("vessel", new LiteralContextValueProvider("application"), ContextScope.Application))
            .Declare(new ContextValueDeclaration("vessel", new LiteralContextValueProvider("module"), ContextScope.Of("Fleet")))
            .Declare(new ContextValueDeclaration("vessel", new LiteralContextValueProvider("feature"), ContextScope.Of("Fleet", "DataQuality")));
        _slice = await Requests.For(null, catalog, scope: ContextScope.Of("Fleet", "DataQuality", "Scores")).Resolve("vessel");
        _sibling = await Requests.For(null, catalog, scope: ContextScope.Of("Fleet", "Other")).Resolve("vessel");
        _application = await Requests.For(null, catalog, scope: ContextScope.Of("Billing")).Resolve("vessel");
        _winner = catalog.Find("vessel", ContextScope.Of("Fleet", "DataQuality", "Scores"));
    }

    [Fact] void should_use_the_nearest_declaration() => _slice.Value.ShouldEqual("feature");
    [Fact] void should_inherit_the_module_declaration_in_a_sibling_feature() => _sibling.Value.ShouldEqual("module");
    [Fact] void should_inherit_the_application_declaration_elsewhere() => _application.Value.ShouldEqual("application");
    [Fact] void should_tell_which_declaration_wins() => _winner.Scope.Path.Count.ShouldEqual(2);
}
