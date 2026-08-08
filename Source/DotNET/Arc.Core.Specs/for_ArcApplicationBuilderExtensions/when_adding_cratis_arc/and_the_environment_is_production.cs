// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_ArcApplicationBuilderExtensions.when_adding_cratis_arc;

/// <summary>
/// Outside Development the host leaves <see cref="ServiceProviderOptions.ValidateScopes"/> off, and Arc keeps that
/// value rather than deciding for the application that every resolution should be checked.
/// </summary>
[Collection("UsesCurrentDirectory")]
public class and_the_environment_is_production : Specification
{
    ArcApplication? _app;
    Exception? _buildError;
    Exception? _resolveError;

    void Because()
    {
        if (!Directory.Exists(Environment.CurrentDirectory))
        {
            Environment.CurrentDirectory = AppContext.BaseDirectory;
        }

        var builder = new ArcApplicationBuilder(["--environment=Production"]);
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
