// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Chronicle.for_ArcBuilderExtensions.given;

public class a_chronicle_host : Specification
{
    protected const string EventStore = "unit-of-work-registration";
    protected IHost _host;
    protected IServiceCollection _services;

    protected static void ConfigureChronicle(IArcBuilder builder)
    {
        // Keep transport and artifact discovery outside this DI composition specification.
        // The client, event stores and transaction managers are the real Chronicle implementations.
        builder.Services.AddSingleton(Substitute.For<IChronicleConnection>());
        builder.WithChronicle(
            options =>
            {
                options.EventStore = EventStore;
                options.AutoDiscoverAndRegister = false;
                options.UnitOfWorkLifecyclePolicy = UnitOfWorkLifecyclePolicy.Strict;
            },
            chronicle => chronicle.WithArtifactsProvider(Substitute.For<IClientArtifactsProvider>()));
    }

    async Task Destroy()
    {
        if (_host is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
        else
        {
            _host?.Dispose();
        }
    }
}
