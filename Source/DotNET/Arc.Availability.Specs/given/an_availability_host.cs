// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Tenancy;
using Cratis.Choreography.Availability;
using Cratis.Choreography.Availability.Audiences;
using Cratis.Choreography.Availability.Context;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability.given;

public class an_availability_host : Specification
{
    protected InMemoryAudienceSource _audiences;
    protected ServiceProvider _root;
    protected IServiceScope _scope;
    protected IAvailabilityGate _gate;

    protected virtual IEnumerable<Type> Types => [typeof(PlusCommand), typeof(UngatedCommand), typeof(Fleet.Quality.ScoresReadModel), typeof(Fleet.Billing.InvoiceCommand)];

    protected virtual IAudienceSource AudienceSource => _audiences;

    protected virtual ContextValueCatalog ValueCatalog => new();

    protected virtual void Configure(ArcAvailabilityOptions options)
    {
        options
            .GateNamespace("Cratis.Arc.Availability.given.Fleet", AvailabilityExpression.For("Preview", "Plus"))
            .GateNamespace("Cratis.Arc.Availability.given.Fleet.Quality", AvailabilityExpression.For("Plus"));
    }

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
            .AddSingleton(AudienceSource)
            .AddSingleton<IDeclaredAudiences>(new DeclaredAudiences("Plus", "Preview"))
            .AddSingleton(new AvailabilityCatalog())
            .AddSingleton(ValueCatalog)
            .AddArcAvailability(Configure)
            .BuildServiceProvider(true);
        _scope = _root.CreateScope();
        _gate = _root.GetRequiredService<IAvailabilityGate>();
    }

    void Destroy()
    {
        _scope.Dispose();
        _root.Dispose();
    }

    [AvailableFor("Plus")]
    public record PlusCommand;

    public record UngatedCommand;
}
