// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationStartupValidation;

[Collection("UsesCurrentDirectory")]
public class when_an_anonymous_aspnet_opt_in_is_invalid : Specification
{
    Exception? _unknown;
    Exception? _native;
    Exception? _requiresAuthentication;
    Exception? _duplicate;
    Exception? _caseInsensitive;
    Exception? _wrongCaseWithCaseSensitiveProvider;
    Exception? _distinctNamesWithCaseSensitiveProvider;

    async Task Because()
    {
        _unknown = await Start(services => services.AddArcAnonymousAspNetAuthorizationPolicy("Missing"));
        _native = await Start(services =>
        {
            services.AddArcAuthorizationPolicy<AllowGuest>("Native", evaluatesAnonymous: true);
            services.AddArcAnonymousAspNetAuthorizationPolicy("native");
        });
        _requiresAuthentication = await Start(services =>
        {
            services.AddAuthorizationBuilder().AddPolicy("Private", policy => policy.RequireAuthenticatedUser().RequireAssertion(_ => true));
            services.AddArcAnonymousAspNetAuthorizationPolicy("Private");
        });
        _duplicate = await Start(services =>
        {
            services.AddAuthorizationBuilder().AddPolicy("Public", policy => policy.RequireAssertion(_ => true));
            services.AddArcAnonymousAspNetAuthorizationPolicy("Public");
            services.AddArcAnonymousAspNetAuthorizationPolicy("Public");
        });
        _caseInsensitive = await Start(services =>
        {
            services.AddAuthorizationBuilder().AddPolicy("Public", policy => policy.RequireAssertion(_ => true));
            services.AddArcAnonymousAspNetAuthorizationPolicy("public");
        });
        _wrongCaseWithCaseSensitiveProvider = await Start(services =>
        {
            AddCaseSensitiveProvider(services);
            services.AddArcAnonymousAspNetAuthorizationPolicy("GUEST");
        });
        _distinctNamesWithCaseSensitiveProvider = await Start(services =>
        {
            AddCaseSensitiveProvider(services);
            services.AddArcAnonymousAspNetAuthorizationPolicy("Guest");
            services.AddArcAnonymousAspNetAuthorizationPolicy("guest");
        });
    }

    [Fact] void should_reject_an_unknown_opt_in_at_startup() => _unknown.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();
    [Fact] void should_reject_a_native_opt_in_even_when_casing_differs() => _native.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();
    [Fact] void should_reject_deny_anonymous_even_when_other_requirements_allow_it() => _requiresAuthentication.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();
    [Fact] void should_reject_ambiguous_opt_ins() => _duplicate.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();
    [Fact] void should_accept_a_name_resolved_by_the_default_provider() => _caseInsensitive.ShouldBeNull();
    [Fact] void should_reject_a_name_the_case_sensitive_provider_does_not_resolve() => _wrongCaseWithCaseSensitiveProvider.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();
    [Fact] void should_accept_distinct_names_resolved_by_the_case_sensitive_provider() => _distinctNamesWithCaseSensitiveProvider.ShouldBeNull();

    static void AddCaseSensitiveProvider(IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationPolicyProvider>(new CaseSensitivePolicyProvider());
    }

    static async Task<Exception?> Start(Action<IServiceCollection> configure)
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        configure(builder.Services);
        var handlers = Substitute.For<ICommandHandlerProviders>();
        handlers.Handlers.Returns([]);
        var performers = Substitute.For<IQueryPerformerProviders>();
        performers.Performers.Returns([]);
        builder.Services.AddSingleton(handlers);
        builder.Services.AddSingleton(performers);
        await using var app = builder.Build();
        app.UseCratisArc();
        var failure = await Catch.Exception(() => app.StartAsync());
        if (failure is null)
        {
            await app.StopAsync();
        }
        return failure;
    }

    class CaseSensitivePolicyProvider : IAuthorizationPolicyProvider
    {
        readonly Dictionary<string, AuthorizationPolicy> _policies = new(StringComparer.Ordinal)
        {
            ["Guest"] = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build(),
            ["guest"] = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build()
        };

        public bool AllowsCachingPolicies => false;

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName) =>
            Task.FromResult(_policies.GetValueOrDefault(policyName));

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
            Task.FromResult(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => Task.FromResult<AuthorizationPolicy?>(null);
    }

    public class AllowGuest : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken) => ValueTask.FromResult(true);
    }
}
