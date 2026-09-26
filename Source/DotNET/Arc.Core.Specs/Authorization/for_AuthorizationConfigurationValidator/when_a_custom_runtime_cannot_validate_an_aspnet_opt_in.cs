// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationConfigurationValidator;

[Collection("UsesCurrentDirectory")]
public class when_a_custom_runtime_cannot_validate_an_aspnet_opt_in : Specification
{
    Exception? _failure;

    async Task Because()
    {
        var builder = ArcApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.Services.AddArcAnonymousAspNetAuthorizationPolicy("Public");
        builder.Services.AddSingleton(Substitute.For<IAuthorizationPolicyRuntime>());
        var handlers = Substitute.For<ICommandHandlerProviders>();
        handlers.Handlers.Returns([]);
        var performers = Substitute.For<IQueryPerformerProviders>();
        performers.Performers.Returns([]);
        builder.Services.AddSingleton(handlers);
        builder.Services.AddSingleton(performers);
        await using var app = builder.Build();
        _failure = await Catch.Exception(() => app.StartAsync());
    }

    [Fact] void should_reject_the_unverifiable_opt_in_at_startup() => _failure.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();
}
