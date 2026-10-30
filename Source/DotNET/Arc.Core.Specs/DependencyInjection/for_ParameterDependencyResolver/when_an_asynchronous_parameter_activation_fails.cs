// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ParameterDependencyResolver;

public class when_an_asynchronous_parameter_activation_fails : Specification
{
    ServiceProvider _services;
    when_resolving_an_asynchronous_parameter.AsyncResolver _resolver;
    readonly InvalidOperationException _activation = new("Activation failed");
    Exception? _failure;

    void Establish()
    {
        _resolver = new();
        _services = new ServiceCollection()
            .AddSingleton<IInstancesOf<ICanResolveParameterDependency>>(new KnownInstancesOf<ICanResolveParameterDependency>([_resolver]))
            .BuildServiceProvider();
    }

    async Task Because()
    {
        var pending = ParameterDependencyResolver.ResolveAsync(_services, typeof(Consumer).GetMethod(nameof(Consumer.Method))!.GetParameters()[0], _ => new Exception());
        _resolver.Completion.SetException(_activation);
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

    [Fact] void should_translate_activation_failures() => _failure.ShouldBeOfExactType<CannotResolveDependency>();
    [Fact] void should_preserve_the_original_failure() => _failure!.InnerException.ShouldBeSame(_activation);
}
