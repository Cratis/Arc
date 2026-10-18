// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Validation;
using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.ReadModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Chronicle.DependencyInjection.for_ProtectedDecisionServiceProviderFactory.when_hosted;

public class with_reader_registrations_in_application_callbacks : for_ArcBuilderExtensions.given.a_chronicle_host
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_refuse_readers_added_by_either_application_callback(bool inOptions)
    {
        var builder = WebApplication.CreateBuilder();
        var factoryCalls = 0;
        var callbacks = 0;
        var applicationReader = ServiceDescriptor.Scoped<IDecisionReads>(_ =>
        {
            factoryCalls++;
            return Substitute.For<IDecisionReads>();
        });
        builder.AddCratisArc(configureBuilder: arc =>
        {
            arc.Services.AddSingleton(Substitute.For<IChronicleConnection>());
            arc.WithChronicle(
                options =>
                {
                    options.EventStore = EventStore;
                    options.AutoDiscoverAndRegister = false;
                    if (inOptions) builder.Services.Add(applicationReader);
                },
                chronicle =>
                {
                    chronicle.WithArtifactsProvider(Substitute.For<IClientArtifactsProvider>());
                    if (!inOptions) builder.Services.Add(applicationReader);
                    if (inOptions)
                    {
                        // Chronicle normally defers options. Exercise an application that requests them during registration.
                        var configurations = builder.Services.Where(binding => binding.ServiceType == typeof(IConfigureOptions<ChronicleClientOptions>))
                            .Select(binding => binding.ImplementationInstance).OfType<IConfigureOptions<ChronicleClientOptions>>().ToArray();
                        configurations.ShouldNotBeEmpty();
                        foreach (var configuration in configurations) configuration.Configure(new ChronicleClientOptions());
                    }
                });
        });
        builder.Services.AddProtectedDecisionValidator<ReadingValidator>(_ => callbacks++);
        builder.Host.UseServiceProviderFactory(new ProtectedDecisionServiceProviderFactory());
        _host = builder.Build();
        await using var scope = _host.Services.CreateAsyncScope();
        ProtectedDecisionProviderAttestations.TryGetAttestation(scope.ServiceProvider, out var attestation).ShouldBeTrue();
        builder.Services.Contains(applicationReader).ShouldBeTrue();
        attestation.Root.Manifest.OriginOf(applicationReader).ShouldBeNull();
        attestation.Root.Manifest.HasTrustedReader().ShouldBeFalse();
        using var policy = CommandDecisionPolicy.Begin(typeof(Model));
        await using var frame = new ProtectedValidatorActivationFrame();

        var error = Catch.Exception(() => CertifiedProtectedValidatorConstruction.Construct(scope.ServiceProvider, typeof(ReadingValidator), true));

        error.ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
        factoryCalls.ShouldEqual(0);
        callbacks.ShouldEqual(0);
    }

    [ProtectedDecision]
    public record Model;

    public class ReadingValidator(IDecisionReads reads) : CommandValidator<Model>
    {
        public IDecisionReads Reads { get; } = reads;
    }
}
