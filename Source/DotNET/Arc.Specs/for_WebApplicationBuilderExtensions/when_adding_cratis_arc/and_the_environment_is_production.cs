// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Builder.for_WebApplicationBuilderExtensions.when_adding_cratis_arc;

/// <summary>
/// Preserving <see cref="ServiceProviderOptions.ValidateScopes"/> means preserving the host's value for it, not
/// forcing it on. Outside Development the host leaves scope validation off, and Arc has no business turning a
/// per-resolution check on in production on the application's behalf.
/// </summary>
public class and_the_environment_is_production : Specification
{
    WebApplication? _app;
    Exception? _buildError;
    Exception? _resolveError;

    void Because()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
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
