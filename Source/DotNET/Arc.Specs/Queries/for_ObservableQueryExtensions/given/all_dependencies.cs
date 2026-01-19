// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ObservableQueryExtensions.given;

public class all_dependencies : Specification
{
    protected IServiceProvider _serviceProvider;
    protected IWebSocketConnectionHandler _webSocketConnectionHandler;
    protected ILogger<IClientObservable> _logger;

    void Establish()
    {
        var services = new ServiceCollection();
        _webSocketConnectionHandler = Substitute.For<IWebSocketConnectionHandler>();
        _logger = Substitute.For<ILogger<IClientObservable>>();

        services.AddSingleton(_webSocketConnectionHandler);
        services.AddSingleton(_logger);
        services.AddSingleton(Substitute.For<Microsoft.Extensions.Hosting.IHostApplicationLifetime>());
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
    }
}
