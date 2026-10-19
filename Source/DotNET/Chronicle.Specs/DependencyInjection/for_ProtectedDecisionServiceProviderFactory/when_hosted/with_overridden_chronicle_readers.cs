// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.DependencyInjection;
using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Chronicle.DependencyInjection.for_ProtectedDecisionServiceProviderFactory.when_hosted;

public class with_overridden_chronicle_readers : for_ArcBuilderExtensions.given.a_chronicle_host
{
    [Theory]
    [InlineData("type", false)]
    [InlineData("factory", false)]
    [InlineData("instance", false)]
    [InlineData("type", true)]
    [InlineData("factory", true)]
    [InlineData("instance", true)]
    [InlineData("raw factory", false)]
    [InlineData("raw factory", true)]
    public void should_refuse_every_unrecorded_reader_including_superseded_application_bindings(string registration, bool beforeArc)
    {
        var builder = WebApplication.CreateBuilder();
        if (beforeArc) Register(builder.Services, registration);
        builder.AddCratisArc(configureBuilder: ConfigureChronicle);
        if (!beforeArc) Register(builder.Services, registration);
        builder.Host.UseServiceProviderFactory(new ProtectedDecisionServiceProviderFactory());
        _host = builder.Build();
        using var scope = _host.Services.CreateScope();
        ProtectedDecisionProviderAttestations.TryGetAttestation(scope.ServiceProvider, out var attestation).ShouldBeTrue();
        attestation.Root.Manifest.HasTrustedReader().ShouldBeFalse();
    }

    static void Register(IServiceCollection services, string registration)
    {
        if (registration == "type") services.AddScoped<IDecisionReads, CommandDecisionReads>();
        if (registration == "factory") services.AddScoped(_ => Substitute.For<IDecisionReads>());
        if (registration == "raw factory") services.AddScoped(provider => provider.GetRequiredService<IEventStore>().GetDecisionReads());
        if (registration == "instance") services.AddSingleton(Substitute.For<IDecisionReads>());
    }
}
