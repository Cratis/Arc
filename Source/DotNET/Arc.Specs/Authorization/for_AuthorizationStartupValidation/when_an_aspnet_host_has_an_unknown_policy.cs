// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Authorization.for_AuthorizationStartupValidation;

[Collection("UsesCurrentDirectory")]
public class when_an_aspnet_host_has_an_unknown_policy : Specification
{
    Exception? _failure;
    bool _laterHostedServiceStarted;

    async Task Because()
    {
        var handlers = Substitute.For<ICommandHandlerProviders>();
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(typeof(UnknownPolicyCommand));
        handlers.Handlers.Returns([handler]);
        var performers = Substitute.For<IQueryPerformerProviders>();
        performers.Performers.Returns([]);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.AddCratisArc();
        builder.Services.AddSingleton(handlers);
        builder.Services.AddSingleton(performers);
        builder.Services.AddSingleton<IHostedService>(new LaterHostedService(() => _laterHostedServiceStarted = true));
        await using var app = builder.Build();
        app.UseCratisArc();
        _failure = await Catch.Exception(() => app.StartAsync());
    }

    [Fact] void should_fail_startup() => _failure.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();
    [Fact] void should_not_start_later_hosted_services() => _laterHostedServiceStarted.ShouldBeFalse();

    [Authorize(Policy = "Unregistered")]
    public record UnknownPolicyCommand;

    public class LaterHostedService(Action onStart) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            onStart();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
