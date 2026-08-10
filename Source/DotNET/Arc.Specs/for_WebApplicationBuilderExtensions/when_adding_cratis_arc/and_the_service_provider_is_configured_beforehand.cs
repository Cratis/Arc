// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Builder.for_WebApplicationBuilderExtensions.when_adding_cratis_arc;

/// <summary>
/// The ordering the documentation warns about. Both <c>UseDefaultServiceProvider</c> overloads hand their
/// callback a brand new <see cref="ServiceProviderOptions"/> and the host keeps only the last call, so an
/// application stating its choice before <c>AddCratisArc</c> has it replaced by Arc's. The behavior is the
/// host's, not something Arc can change without owning the setting outright — this spec exists so the
/// documented answer and the real one cannot drift apart.
/// </summary>
public class and_the_service_provider_is_configured_beforehand : Specification
{
    WebApplication? _app;
    Exception? _buildError;
    Exception? _resolveError;

    void Because()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = false;
        });
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
    [Fact] void should_leave_a_singleton_that_captures_a_scoped_service_alone() => _resolveError.ShouldBeNull();

    class ScopedCollaborator;

    class SingletonHoldingAScopedCollaborator(ScopedCollaborator collaborator)
    {
        public ScopedCollaborator Collaborator => collaborator;
    }
}
