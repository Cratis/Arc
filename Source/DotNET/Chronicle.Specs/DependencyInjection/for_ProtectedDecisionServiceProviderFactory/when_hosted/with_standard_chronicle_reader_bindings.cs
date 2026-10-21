// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.DependencyInjection;
using Cratis.Chronicle.ReadModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Chronicle.DependencyInjection.for_ProtectedDecisionServiceProviderFactory.when_hosted;

public class with_standard_chronicle_reader_bindings : for_ArcBuilderExtensions.given.a_chronicle_host
{
    ServiceDescriptor[] _bindings;
    Type _effectiveReaderType;
    ProtectedDecisionRegistrationManifest _manifest;

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc(configureBuilder: ConfigureChronicle);
        builder.Host.UseServiceProviderFactory(new ProtectedDecisionServiceProviderFactory());
        _services = builder.Services;
        _host = builder.Build();
    }

    void Because()
    {
        using var scope = _host.Services.CreateScope();
        _bindings = _services.Where(descriptor => descriptor.ServiceType == typeof(IDecisionReads)).ToArray();
        _effectiveReaderType = scope.ServiceProvider.GetRequiredService<IDecisionReads>().GetType();
        ProtectedDecisionProviderAttestations.TryGetAttestation(scope.ServiceProvider, out var attestation).ShouldBeTrue();
        _manifest = attestation!.Root.Manifest;
    }

    [Fact] void should_have_reader_bindings() => _bindings.ShouldNotBeEmpty();
    [Fact] void should_select_the_command_aware_reader() => _effectiveReaderType.ShouldEqual(typeof(CommandDecisionReads));
    [Fact] void should_accept_the_standard_composition() => _manifest.HasTrustedReader().ShouldBeTrue();
    [Fact]
    void should_record_every_reader_origin()
    {
        foreach (var binding in _bindings)
        {
            if (_manifest.OriginOf(binding) is null)
            {
                throw new InvalidOperationException($"Unrecorded framework reader: {binding}");
            }
        }
    }
    [Fact] void should_record_readers_added_by_the_chronicle_client_call() => _bindings.Any(binding => _manifest.OriginOf(binding)!.IsChronicleClientReader).ShouldBeTrue();
    [Fact] void should_retain_the_superseded_framework_reader() => _bindings.Any(binding => _manifest.OriginOf(binding)!.IsSupersededReader).ShouldBeTrue();
}
