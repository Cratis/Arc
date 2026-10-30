// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using Cratis.Arc.DependencyInjection.for_ParameterDependencyResolver;

namespace Cratis.Arc.Commands.for_CommandProvideInvoker;

public class when_resolving_an_asynchronous_read_model : given.a_command_provide_invoker
{
    when_resolving_an_asynchronous_parameter.AsyncResolver _dependencyResolver;
    TheDependency _dependency;
    IReadOnlyList<object> _result;
    bool _yielded;
    CancellationToken _cancellation;
    Consumer _command;

    void Establish()
    {
        _dependency = new();
        _dependencyResolver = new();
        _cancellation = new CancellationToken(true);
        _command = new();
        _serviceProvider.GetService(typeof(IInstancesOf<ICanResolveParameterDependency>))
            .Returns(new KnownInstancesOf<ICanResolveParameterDependency>([_dependencyResolver]));
    }

    async Task Because()
    {
        var pending = Invoke(_command, _cancellation);
        _yielded = !pending.IsCompleted;
        _dependencyResolver.Completion.SetResult(_dependency);
        _result = await pending;
    }

    [Fact] void should_yield_while_acquiring_the_dependency() => _yielded.ShouldBeTrue();
    [Fact] void should_return_the_provided_dependency() => _result[0].ShouldBeSame(_dependency);
    [Fact] void should_preserve_cancellation_token_binding() => _command.Cancellation.ShouldEqual(_cancellation);

    public class Consumer
    {
        public CancellationToken Cancellation { get; private set; }
        public TheDependency Provide(TheDependency dependency, CancellationToken cancellation)
        {
            Cancellation = cancellation;
            return dependency;
        }
    }
}
