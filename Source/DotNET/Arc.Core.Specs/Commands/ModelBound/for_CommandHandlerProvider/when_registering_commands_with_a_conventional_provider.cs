// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Commands.ModelBound.for_CommandHandlerProvider;

public class when_registering_commands_with_a_conventional_provider : Specification
{
    ServiceProvider _container;
    CommandHandlerProvider _concrete;
    ICommandHandlerProvider _conventional;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.All.Returns([typeof(InternalCommand)]);
        var services = new ServiceCollection()
            .AddSingleton(types)
            .AddSingleton(new ArtifactAssemblyFilter([], _ => true))
            .AddSingleton<ICommandHandlerProvider, CommandHandlerProvider>();
        services.AddCratisCommands();
        _container = services.BuildServiceProvider();
    }

    void Because()
    {
        _concrete = _container.GetRequiredService<CommandHandlerProvider>();
        _conventional = _container.GetRequiredService<ICommandHandlerProvider>();
    }

    void Destroy() => _container.Dispose();

    [Fact] void should_share_the_filtered_provider() => _conventional.ShouldBeSame(_concrete);
    [Fact] void should_filter_the_concrete_provider() => _concrete.Handlers.ShouldBeEmpty();
    [Fact] void should_filter_the_conventional_provider() => _conventional.Handlers.ShouldBeEmpty();
}
