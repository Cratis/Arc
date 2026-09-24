// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationStartupValidation;

[Collection("UsesCurrentDirectory")]
public class when_an_aspnet_host_has_an_unknown_scheme : Specification
{
    Exception? _failure;

    async Task Because()
    {
        var handlers = Substitute.For<ICommandHandlerProviders>();
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(typeof(UnknownSchemeCommand));
        handlers.Handlers.Returns([handler]);
        var performers = Substitute.For<IQueryPerformerProviders>();
        performers.Performers.Returns([]);

        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.Services.AddSingleton(handlers);
        builder.Services.AddSingleton(performers);
        await using var app = builder.Build();
        app.UseCratisArc();
        _failure = await Catch.Exception(() => app.StartAsync());
    }

    [Fact] void should_reject_the_unknown_scheme_before_startup() => _failure.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();

    [Authorize(AuthenticationSchemes = "UnknownScheme")]
    public record UnknownSchemeCommand;
}
