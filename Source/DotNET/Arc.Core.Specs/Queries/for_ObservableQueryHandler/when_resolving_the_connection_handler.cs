// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryHandler;

public class when_resolving_the_connection_handler : Specification
{
    ServiceProvider _provider;
    IWebSocketConnectionHandler _handler;

    void Because()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<ArcOptions>();
        services.AddArcServiceBindings();
        _provider = services.BuildServiceProvider();
        _handler = _provider.GetRequiredService<IWebSocketConnectionHandler>();
    }

    [Fact] void should_resolve_the_production_handler() => _handler.ShouldBeOfExactType<WebSocketConnectionHandler>();
    [Fact] void should_expose_outcome_aware_writes() => (_handler is IWebSocketMessageSender).ShouldBeTrue();

    void Destroy() => _provider?.Dispose();
}
