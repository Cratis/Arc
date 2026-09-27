// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.ModelBound.for_CommandHandlerProvider;

public class when_building_containers_over_shared_and_distinct_universes : Specification
{
    ITypes _sharedUniverse;
    ITypes _otherUniverse;
    ConcurrentBag<CommandHandlerProvider> _providers;
    CommandHandlerProvider _otherProvider;
    CommandHandlerProvider _updatedProvider;

    void Establish()
    {
        _sharedUniverse = Substitute.For<ITypes>();
        _sharedUniverse.All.Returns([typeof(InternalCommand)]);
        _otherUniverse = Substitute.For<ITypes>();
        _otherUniverse.All.Returns([]);
        _providers = [];
    }

    void Because()
    {
        Parallel.For(0, 24, _ =>
        {
            using var container = new ServiceCollection()
                .AddSingleton(_sharedUniverse)
                .AddSingleton<CommandHandlerProvider>()
                .BuildServiceProvider();
            _providers.Add(container.GetRequiredService<CommandHandlerProvider>());
        });

        using var otherContainer = new ServiceCollection()
            .AddSingleton(_otherUniverse)
            .AddSingleton<CommandHandlerProvider>()
            .BuildServiceProvider();
        _otherProvider = otherContainer.GetRequiredService<CommandHandlerProvider>();
        _otherUniverse.All.Returns([typeof(InternalCommand)]);
        _updatedProvider = new CommandHandlerProvider(_otherUniverse);
    }

    [Fact] void should_check_the_shared_universe_for_changes() => _ = _sharedUniverse.Received(24).All;
    [Fact] void should_create_a_provider_for_every_container() => _providers.Count.ShouldEqual(24);
    [Fact] void should_keep_handlers_for_the_shared_universe() => _providers.All(provider => provider.Handlers.Count() == 1).ShouldBeTrue();
    [Fact] void should_check_the_other_universe_for_changes() => _ = _otherUniverse.Received(2).All;
    [Fact] void should_not_reuse_handlers_from_the_shared_universe() => _otherProvider.Handlers.ShouldBeEmpty();
    [Fact] void should_rediscover_when_the_same_universe_changes() => _updatedProvider.Handlers.Count().ShouldEqual(1);
}
