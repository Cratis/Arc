// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Availability.Audiences;
using Cratis.Arc.Tenancy;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability.given;

/// <summary>
/// A host that discovers its audiences, blocks and gates from the types of the application.
/// </summary>
public class a_typed_availability_host : Specification
{
    protected InMemoryAudienceSource _audiences;
    protected ServiceProvider _root;
    protected IServiceScope _scope;
    protected IAvailabilityGate _gate;

    protected IEnumerable<Type> Types =>
    [
        typeof(TypedDeclarations.Plus),
        typeof(TypedDeclarations.Preview),
        typeof(TypedDeclarations.Incident),
        typeof(TypedDeclarations.Foundation),
        typeof(TypedDeclarations.QualityScores),
        typeof(TypedDeclarations.EitherOrBlock),
        typeof(TypedDeclarations.ConditionsOfOneWhen),
        typeof(TypedDeclarations.WithAnOpaqueCondition),
        typeof(TypedDeclarations.ScoresCommand),
        typeof(TypedDeclarations.EitherAudienceCommand),
        typeof(TypedDeclarations.BothAudiencesCommand),
        typeof(TypedDeclarations.OpaqueCommand),
        typeof(GatedNamespace.GatedFeature),
        typeof(GatedNamespace.InsideTheFeature)
    ];

    void Establish()
    {
        _audiences = new InMemoryAudienceSource();
        var types = Substitute.For<ITypes>();
        types.All.Returns(Types.ToArray());
        var tenant = Substitute.For<ITenantIdAccessor>();
        tenant.Current.Returns(new TenantId("acme"));
        var principal = Substitute.For<ICurrentPrincipalAccessor>();
        principal.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "alice")], "test")));

        _root = new ServiceCollection()
            .AddSingleton(types)
            .AddSingleton(tenant)
            .AddSingleton(principal)
            .AddSingleton<IAudienceSource>(_audiences)
            .AddArcAvailability()
            .BuildServiceProvider(true);
        _scope = _root.CreateScope();
        _gate = _root.GetRequiredService<IAvailabilityGate>();
    }

    void Destroy()
    {
        _scope.Dispose();
        _root.Dispose();
    }
}
