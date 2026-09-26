// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationStartupValidation;

[Collection("UsesCurrentDirectory")]
public class when_validating_multiple_aspnet_anonymous_opt_ins : Specification
{
    int _publicResolutions;
    int _guestResolutions;
    Exception? _failure;

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.Services.AddArcAnonymousAspNetAuthorizationPolicy("Public");
        builder.Services.AddArcAnonymousAspNetAuthorizationPolicy("Guest");
        var provider = new CountingPolicyProvider();
        builder.Services.AddSingleton<IAuthorizationPolicyProvider>(provider);
        var handlers = Substitute.For<ICommandHandlerProviders>();
        var first = Substitute.For<ICommandHandler>();
        first.CommandType.Returns(typeof(FirstCommand));
        var second = Substitute.For<ICommandHandler>();
        second.CommandType.Returns(typeof(SecondCommand));
        handlers.Handlers.Returns([first, second]);
        var performers = Substitute.For<IQueryPerformerProviders>();
        performers.Performers.Returns([]);
        builder.Services.AddSingleton(handlers);
        builder.Services.AddSingleton(performers);
        await using var app = builder.Build();
        app.UseCratisArc();
        _failure = await Catch.Exception(() => app.StartAsync());
        _publicResolutions = provider.PublicResolutions;
        _guestResolutions = provider.GuestResolutions;
        if (_failure is null)
        {
            await app.StopAsync();
        }
    }

    [Fact] void should_start_successfully() => _failure.ShouldBeNull();
    [Fact] void should_resolve_public_once() => _publicResolutions.ShouldEqual(1);
    [Fact] void should_resolve_guest_once() => _guestResolutions.ShouldEqual(1);

    public record FirstCommand;
    public record SecondCommand;

    class CountingPolicyProvider : IAuthorizationPolicyProvider
    {
        readonly AuthorizationPolicy _policy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();
        public int PublicResolutions { get; private set; }
        public int GuestResolutions { get; private set; }
        public bool AllowsCachingPolicies => false;

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (policyName == "Public")
            {
                PublicResolutions++;
            }

            if (policyName == "Guest")
            {
                GuestResolutions++;
            }

            return Task.FromResult<AuthorizationPolicy?>(_policy);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => Task.FromResult(_policy);
        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => Task.FromResult<AuthorizationPolicy?>(null);
    }
}
