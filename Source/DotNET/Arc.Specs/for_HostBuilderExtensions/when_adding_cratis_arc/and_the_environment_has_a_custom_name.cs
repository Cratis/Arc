// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting.for_HostBuilderExtensions.when_adding_cratis_arc;

/// <summary>
/// <c>IsDevelopment()</c> is an exact match on the name <c>Development</c>, so a host running as <c>Local</c>
/// gets no scope validation — the same answer a bare generic host gives.
/// </summary>
public class and_the_environment_has_a_custom_name : Specification
{
    IHost? _host;
    Exception? _buildError;
    Exception? _resolveError;

    void Because()
    {
        var builder = new HostBuilder()
            .ConfigureDefaults([])
            .UseEnvironment("Local");

        builder.AddCratisArc(options => options.IdentityDetailsProvider = typeof(DefaultIdentityDetailsProvider));

        builder.ConfigureServices(services =>
        {
            services.AddScoped<ScopedCollaborator>();
            services.AddSingleton<SingletonHoldingAScopedCollaborator>();
        });

        _buildError = Catch.Exception(() => _host = builder.Build());
        _resolveError = _host is null ? null : Catch.Exception(ResolveTheCaptorFromAScope);
    }

    void ResolveTheCaptorFromAScope()
    {
        using var scope = _host!.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<SingletonHoldingAScopedCollaborator>();
    }

    void Destroy() => _host?.Dispose();

    [Fact] void should_build_without_eagerly_validating_every_registration() => _buildError.ShouldBeNull();
    [Fact] void should_leave_a_singleton_that_captures_a_scoped_service_alone() => _resolveError.ShouldBeNull();

    class ScopedCollaborator;

    class SingletonHoldingAScopedCollaborator(ScopedCollaborator collaborator)
    {
        public ScopedCollaborator Collaborator => collaborator;
    }
}
