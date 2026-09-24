// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationStartupValidation;

[Collection("UsesCurrentDirectory")]
public class when_a_policy_name_is_registered_on_both_hosts : Specification
{
    Exception? _failure;

    async Task Because()
    {
        var handlers = Substitute.For<ICommandHandlerProviders>();
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(typeof(AmbiguousPolicyCommand));
        handlers.Handlers.Returns([handler]);
        var performers = Substitute.For<IQueryPerformerProviders>();
        performers.Performers.Returns([]);
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.Services.AddArcAuthorizationPolicy<AlwaysAllow>("Shared");
        builder.Services.AddAuthorizationBuilder().AddPolicy("Shared", policy => policy.RequireAuthenticatedUser());
        builder.Services.AddSingleton(handlers);
        builder.Services.AddSingleton(performers);
        await using var app = builder.Build();
        app.UseCratisArc();
        _failure = await Catch.Exception(() => app.StartAsync());
    }

    [Fact] void should_reject_ambiguous_names_before_startup() => _failure.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();

    [Authorize(Policy = "Shared")]
    public record AmbiguousPolicyCommand;

    public class AlwaysAllow : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken) => ValueTask.FromResult(true);
    }
}
