// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Contexts;
using Cratis.Choreography.Availability.Context;
using Cratis.Choreography.Availability.Context.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability.given;

public class a_value_host : an_availability_host
{
    protected int _calls;

    protected override ContextValueCatalog ValueCatalog => new ContextValueCatalog()
        .Declare(new ContextValueDeclaration("vessel", new CodeContextValueProvider(Counted("app")), ContextScope.Application))
        .Declare(new ContextValueDeclaration("vessel", new CodeContextValueProvider(Counted("fleet")), ContextScope.Of("Fleet")))
        .Declare(new ContextValueDeclaration("owner", new CodeContextValueProvider((_, _) => ValueTask.FromResult<object?>(null)), ContextScope.Application, Required: false))
        .Declare(new ContextValueDeclaration("project", new CodeContextValueProvider((_, _) => ValueTask.FromResult<object?>(null)), ContextScope.Application))
        .Declare(new ContextValueDeclaration("broken", new CodeContextValueProvider((_, _) => throw new InvalidOperationException("The store is down.")), ContextScope.Application));

    protected override void Configure(ArcAvailabilityOptions options) =>
        options.ScopeFor = part => part.Namespace?.Contains(".Fleet") == true ? ContextScope.Of("Fleet") : ContextScope.Application;

    protected ContextValues ValuesFor(Type? part) => ContextValues.For(_scope.ServiceProvider, () => part);

    Func<IContextValueResolver, CancellationToken, ValueTask<object?>> Counted(string value) => (_, _) =>
    {
        _calls++;
        return ValueTask.FromResult<object?>(value);
    };
}
