// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting.for_HostBuilderExtensions.when_adding_cratis_arc;

/// <summary>
/// The generic host takes the last <c>UseDefaultServiceProvider</c> call, so an application stating its own
/// choice after <c>AddCratisArc</c> wins — the same escape hatch as on the web host, held in place here too.
/// This is a control: no change to Arc's own call reds it, because the ordering is the host's rule. It guards
/// the documented escape hatch against Arc ever moving its call later, which is the shape of regression the
/// matching Arc.Core spec does catch.
/// </summary>
public class and_the_service_provider_is_configured_afterwards : Specification
{
    IHost? _host;
    Exception? _buildError;
    Exception? _resolveError;

    void Because()
    {
        var builder = new HostBuilder()
            .ConfigureDefaults([])
            .UseEnvironment(Environments.Production);

        builder.AddCratisArc(options => options.IdentityDetailsProvider = typeof(DefaultIdentityDetailsProvider));
        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = false;
        });

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
    [Fact] void should_refuse_a_singleton_that_captures_a_scoped_service() => _resolveError.ShouldNotBeNull();

    class ScopedCollaborator;

    class SingletonHoldingAScopedCollaborator(ScopedCollaborator collaborator)
    {
        public ScopedCollaborator Collaborator => collaborator;
    }
}
