// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ParameterDependencyResolver;

public class when_an_asynchronous_nullable_dependency_is_protected : Specification
{
    ServiceProvider _services;
    when_resolving_an_asynchronous_parameter.AsyncResolver _resolver;
    readonly Exception _guardFailure = new("The missing protected dependency was refused");
    Exception? _failure;

    void Establish()
    {
        _resolver = new();
        var support = Substitute.For<ICommandProtectedDecisionSupport>();
        support.When(_ => _.ValidateCommandDependency(typeof(TheDependency), null)).Do(_ => throw _guardFailure);
        _services = new ServiceCollection()
            .AddSingleton(support)
            .AddSingleton<IInstancesOf<ICanResolveParameterDependency>>(new KnownInstancesOf<ICanResolveParameterDependency>([_resolver]))
            .BuildServiceProvider();
    }

    async Task Because()
    {
        using var policy = CommandDecisionPolicy.Begin(typeof(Consumer), _services);
        var pending = ParameterDependencyResolver.ResolveAsync(_services, typeof(Consumer).GetMethod(nameof(Consumer.Handle))!.GetParameters()[0], _ => new Exception());
        _resolver.Completion.SetResult(null);
        try
        {
            await pending;
        }
        catch (Exception failure)
        {
            _failure = failure;
        }
    }

    void Destroy() => _services.Dispose();

    [Fact] void should_validate_the_protected_null_before_accepting_nullable_binding() => _failure.ShouldBeSame(_guardFailure);

    [ReadProtection(ReadProtectionMode.On)]
    class Consumer
    {
        public void Handle(TheDependency? dependency) { }
    }
}
