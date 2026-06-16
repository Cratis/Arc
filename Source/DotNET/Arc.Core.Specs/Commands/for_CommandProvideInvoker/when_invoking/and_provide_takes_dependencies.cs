// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandProvideInvoker.when_invoking;

public class and_provide_takes_dependencies : given.a_command_provide_invoker
{
    public interface IDependency
    {
        string Value { get; }
    }

    class Command
    {
        public string Provide(IDependency dependency) => dependency.Value;
    }

    IReadOnlyList<object> _result;

    void Establish()
    {
        var dependency = Substitute.For<IDependency>();
        dependency.Value.Returns("from dependency");
        _serviceProvider.GetService(typeof(IDependency)).Returns(dependency);
    }

    async Task Because() => _result = await Invoke(new Command());

    [Fact] void should_resolve_the_dependency_from_di() => _serviceProvider.Received().GetService(typeof(IDependency));
    [Fact] void should_use_the_dependency() => _result[0].ShouldEqual("from dependency");
}
