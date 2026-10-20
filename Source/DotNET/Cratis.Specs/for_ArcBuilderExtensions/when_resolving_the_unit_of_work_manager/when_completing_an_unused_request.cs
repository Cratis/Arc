// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.for_ArcBuilderExtensions.when_resolving_the_unit_of_work_manager;

[Collection("UsesCurrentDirectory")]
public class when_completing_an_unused_request : given.a_chronicle_web_host
{
    int _resolutions;
    bool _completed;

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc(configureBuilder: ConfigureChronicle);
        builder.Services.AddScoped<IEventStore>(_ =>
        {
            _resolutions++;
            return Substitute.For<IEventStore>();
        });
        _host = builder.Build();
    }

    async Task Because()
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
        using var unit = manager.Begin(Guid.NewGuid());
        await unit.Commit();
        _completed = unit.IsCompleted;
    }

    [Fact] void should_complete_the_request() => _completed.ShouldBeTrue();
    [Fact] void should_not_resolve_an_event_store() => _resolutions.ShouldEqual(0);
}
