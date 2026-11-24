// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;
using Cratis.Arc.Availability.Context.Providers;

namespace Cratis.Arc.Availability.for_ContextValueResolver;

public class when_using_the_provider_kinds : Specification
{
    ContextValueOutcome _literal;
    ContextValueOutcome _expression;
    ContextValueOutcome _host;
    ContextValueOutcome _code;

    async Task Because()
    {
        var host = new InMemoryHostContextValues();
        host.Set("entitlements", "gold");
        var request = Requests.For(
            null,
            new ContextValueCatalog()
                .Declare(new ContextValueDeclaration("pkg", new LiteralContextValueProvider("Plus"), ContextScope.Application))
                .Declare(new ContextValueDeclaration("alias", new ExpressionContextValueProvider("pkg"), ContextScope.Application))
                .Declare(new ContextValueDeclaration("ent", new HostContextValueProvider(host, "entitlements"), ContextScope.Application))
                .Declare(new ContextValueDeclaration(
                    "computed",
                    new CodeContextValueProvider(async (c, t) => (await c.Resolve("pkg", t)).Value + "!", ["pkg"]),
                    ContextScope.Application)));
        _literal = await request.Resolve("pkg");
        _expression = await request.Resolve("alias");
        _host = await request.Resolve("ent");
        _code = await request.Resolve("computed");
    }

    [Fact] void should_provide_a_literal() => _literal.Value.ShouldEqual("Plus");
    [Fact] void should_provide_an_expression() => _expression.Value.ShouldEqual("Plus");
    [Fact] void should_provide_a_host_value() => _host.Value.ShouldEqual("gold");
    [Fact] void should_provide_code() => _code.Value.ShouldEqual("Plus!");
}
