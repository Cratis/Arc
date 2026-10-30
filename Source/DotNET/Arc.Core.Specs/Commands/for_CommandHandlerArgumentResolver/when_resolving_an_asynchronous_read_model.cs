// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using Cratis.Arc.DependencyInjection.for_ParameterDependencyResolver;

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver;

public class when_resolving_an_asynchronous_read_model : given.a_command_handler_argument_resolver
{
    when_resolving_an_asynchronous_parameter.AsyncResolver _dependencyResolver;
    TheDependency _dependency;
    CommandHandlerArgumentResolution _result;
    bool _yielded;

    void Establish()
    {
        _dependency = new();
        _dependencyResolver = new();
        _serviceProvider.GetService(typeof(IInstancesOf<ICanResolveParameterDependency>))
            .Returns(new KnownInstancesOf<ICanResolveParameterDependency>([_dependencyResolver]));
        HandleHasParameters<Consumer>();
    }

    async Task Because()
    {
        var pending = Resolve();
        _yielded = !pending.IsCompleted;
        _dependencyResolver.Completion.SetResult(_dependency);
        _result = await pending;
    }

    [Fact] void should_yield_while_acquiring_the_dependency() => _yielded.ShouldBeTrue();
    [Fact] void should_bind_the_resolved_dependency() => _result.Arguments[0].ShouldBeSame(_dependency);

    class Consumer
    {
        public void Handle(TheDependency dependency) { }
    }
}
