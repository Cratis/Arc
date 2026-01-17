// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ObservableQueryExtensions.given;

public class all_dependencies : Specification
{
    protected IServiceProvider _serviceProvider;
    protected System.Text.Json.JsonSerializerOptions _jsonOptions;
    protected IWebSocketConnectionHandler _webSocketConnectionHandler;
    protected ILogger<IClientObservable> _logger;

    void Establish()
    {
        var services = new ServiceCollection();
        _jsonOptions = new System.Text.Json.JsonSerializerOptions();
        _webSocketConnectionHandler = Substitute.For<IWebSocketConnectionHandler>();
        _logger = Substitute.For<ILogger<IClientObservable>>();

        services.AddSingleton(_jsonOptions);
        services.AddSingleton(_webSocketConnectionHandler);
        services.AddSingleton(_logger);
        services.AddSingleton(Substitute.For<Microsoft.Extensions.Hosting.IHostApplicationLifetime>());
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
    }
}
