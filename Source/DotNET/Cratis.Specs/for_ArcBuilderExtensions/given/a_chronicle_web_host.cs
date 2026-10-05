// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.for_ArcBuilderExtensions.given;

public class a_chronicle_web_host : Specification
{
    protected const string EventStore = "unit-of-work-registration";
    protected WebApplication _host;
    protected IServiceCollection _services;

    protected static void ConfigureChronicle(IArcBuilder builder)
    {
        // Keep transport and artifact discovery outside this DI composition specification.
        builder.Services.AddSingleton(Substitute.For<IChronicleConnection>());
        Microsoft.AspNetCore.Builder.ArcBuilderExtensions.WithChronicle(
            builder,
            options =>
            {
                options.EventStore = EventStore;
                options.AutoDiscoverAndRegister = false;
                options.UnitOfWorkLifecyclePolicy = UnitOfWorkLifecyclePolicy.Strict;
            },
            chronicle => chronicle.WithArtifactsProvider(Substitute.For<IClientArtifactsProvider>()));
    }

    void Destroy() => _host?.DisposeAsync().AsTask().GetAwaiter().GetResult();
}
