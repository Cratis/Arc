// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.ReadModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Chronicle.DependencyInjection.for_ProtectedDecisionServiceProviderFactory.when_hosted;

public class with_application_owned_chronicle_registration : for_ArcBuilderExtensions.given.a_chronicle_host
{
    ProtectedDecisionRegistrationManifest _manifest;
    ServiceDescriptor[] _clientReaders;

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc(configureBuilder: arc =>
        {
            arc.Services.AddSingleton(Substitute.For<IChronicleConnection>());
            var before = new HashSet<ServiceDescriptor>(arc.Services, ReferenceEqualityComparer.Instance);
            arc.AppBuilder.AddCratisChronicle(
                configureOptions: options => options.AutoDiscoverAndRegister = false,
                configure: chronicle => arc.Services.AddReadModels(chronicle.ClientArtifactsProvider));
            _clientReaders = arc.Services.Where(binding => binding.ServiceType == typeof(IDecisionReads) && !before.Contains(binding)).ToArray();
            arc.Services.AddCommandAwareDecisionReads();
        });
        builder.Host.UseServiceProviderFactory(new ProtectedDecisionServiceProviderFactory());
        _host = builder.Build();
    }

    void Because()
    {
        using var scope = _host.Services.CreateScope();
        ProtectedDecisionProviderAttestations.TryGetAttestation(scope.ServiceProvider, out var attestation).ShouldBeTrue();
        _manifest = attestation.Root.Manifest;
    }

    [Fact] void should_refuse_the_new_construction_path() => _manifest.HasTrustedReader().ShouldBeFalse();
    [Fact] void should_leave_the_application_owned_client_factory_unrecorded() => _clientReaders.Any(binding => _manifest.OriginOf(binding) is null).ShouldBeTrue();
    [Fact] void should_still_have_a_recorded_effective_command_reader() => _manifest.OriginOf(_manifest.BindingsFor(typeof(IDecisionReads))[^1])!.IsCommandReader.ShouldBeTrue();
}
