// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Availability.Audiences;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability.for_UnavailableParts;

public class when_deciding_fails : given.an_availability_host
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

    async Task Because() => _error = await Catch.Exception(async () => await _scope.ServiceProvider.GetRequiredService<IUnavailableParts>().Get());

    [Fact] void should_fail_as_not_decided() => _error.ShouldBeOfExactType<AvailabilityNotDecidedException>();
    [Fact] void should_keep_the_cause_out_of_the_message() => _error.Message.ShouldNotContain("store is down");
}
