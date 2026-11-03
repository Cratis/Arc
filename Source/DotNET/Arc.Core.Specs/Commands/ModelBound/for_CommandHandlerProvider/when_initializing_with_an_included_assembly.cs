// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.ModelBound.for_CommandHandlerProvider;

public class when_initializing_with_an_included_assembly : Specification
{
    CommandHandlerProvider _provider;
    ITypes _types;

    void Establish()
    {
        _types = Substitute.For<ITypes>();
        _types.All.Returns([typeof(InternalCommand)]);
    }

    void Because() => _provider = new CommandHandlerProvider(_types, new ArtifactAssemblyFilter([typeof(InternalCommand).Assembly], _ => true));

    [Fact] void should_have_a_handler() => _provider.Handlers.Count().ShouldEqual(1);
    [Fact] void should_find_the_command() => _provider.TryGetHandlerFor(new InternalCommand(), out _).ShouldBeTrue();
}
