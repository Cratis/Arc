// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Builder.for_WebApplicationBuilderExtensions.when_adding_cratis_arc;

/// <summary>
/// Arc turns <see cref="ServiceProviderOptions.ValidateOnBuild"/> off because it supplies registrations
/// contextually that eager validation cannot construct. That must not take
/// <see cref="ServiceProviderOptions.ValidateScopes"/> — which the host enables in Development — down with it:
/// refusing a singleton that captures a scoped service is the cheapest guard an application has against a
/// process-lifetime object holding per-request or per-tenant state.
/// </summary>
public class and_the_environment_is_development : Specification
{
    WebApplication? _app;
    Exception? _buildError;
    Exception? _resolveError;

    void Because()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.AddCratisArc(options => options.IdentityDetailsProvider = typeof(DefaultIdentityDetailsProvider));
        builder.Services.AddScoped<ScopedCollaborator>();
        builder.Services.AddSingleton<SingletonHoldingAScopedCollaborator>();

        _buildError = Catch.Exception(() => _app = builder.Build());
        _resolveError = _app is null ? null : Catch.Exception(ResolveTheCaptorFromAScope);
    }

    void ResolveTheCaptorFromAScope()
    {
        using var scope = _app!.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<SingletonHoldingAScopedCollaborator>();
    }

    void Destroy() => _app?.DisposeAsync().GetAwaiter().GetResult();

    [Fact] void should_build_without_eagerly_validating_every_registration() => _buildError.ShouldBeNull();
    [Fact] void should_refuse_a_singleton_that_captures_a_scoped_service() => _resolveError.ShouldNotBeNull();

    class ScopedCollaborator;

    class SingletonHoldingAScopedCollaborator(ScopedCollaborator collaborator)
    {
        public ScopedCollaborator Collaborator => collaborator;
    }
}
