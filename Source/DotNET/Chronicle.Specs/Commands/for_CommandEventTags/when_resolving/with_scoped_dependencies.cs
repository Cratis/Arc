// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_CommandEventTags.when_resolving;

public class with_scoped_dependencies : Specification
{
    ServiceProvider _root;
    IServiceScope _firstScope;
    IServiceScope _secondScope;
    CommandContext _firstContext;
    CommandContext _secondContext;
    IEnumerable<NamedTag> _firstTags;
    IEnumerable<NamedTag> _secondTags;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.FindMultiple<ICanProvideCommandEventTags>().Returns([typeof(TenantTags<object>)]);
        _root = new ServiceCollection().AddSingleton(types).AddScoped<Tenant>().BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        _firstScope = _root.CreateScope();
        _secondScope = _root.CreateScope();
        _firstContext = new(CorrelationId.New(), typeof(object), new object(), [], [], ServiceProvider: _firstScope.ServiceProvider);
        _secondContext = new(CorrelationId.New(), typeof(object), new object(), [], [], ServiceProvider: _secondScope.ServiceProvider);
    }
    void Because()
    {
        _firstTags = _firstContext.ResolveEventTags();
        _secondTags = _secondContext.ResolveEventTags();
    }
    void Destroy()
    {
        _firstScope.Dispose();
        _secondScope.Dispose();
        _root.Dispose();
    }

    [Fact] void should_use_the_first_command_scope() => _firstTags.ShouldEqual([new NamedTag("tenant", _firstScope.ServiceProvider.GetRequiredService<Tenant>().Id)]);
    [Fact] void should_use_the_second_command_scope() => _secondTags.ShouldEqual([new NamedTag("tenant", _secondScope.ServiceProvider.GetRequiredService<Tenant>().Id)]);
    [Fact] void should_not_capture_the_first_tenant_for_the_second_command() => _firstTags.Single().Value.ShouldNotEqual(_secondTags.Single().Value);

    public class Tenant
    {
        public string Id { get; } = Guid.NewGuid().ToString();
    }

    public class TenantTags<T>(Tenant tenant) : ICanProvideCommandEventTags
    {
        public IEnumerable<NamedTag> GetEventTags(object command) => [new("tenant", tenant.Id)];
    }
}
