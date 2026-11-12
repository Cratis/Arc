// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Availability.Audiences;

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_the_audience_source_fails : given.an_availability_host
{
    Exception _error;

    protected override IAudienceSource AudienceSource
    {
        get
        {
            var source = Substitute.For<IAudienceSource>();
            source.GetGrants(Arg.Any<AudienceSubject>(), Arg.Any<CancellationToken>())
                .Returns<ValueTask<IReadOnlyCollection<AudienceGrant>>>(_ => throw new InvalidOperationException("store is down"));
            return source;
        }
    }

    async Task Because() => _error = await Catch.Exception(async () => await _gate.Decide(typeof(given.an_availability_host.PlusCommand), _scope.ServiceProvider, CancellationToken.None));

    [Fact] void should_throw_instead_of_deciding() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
