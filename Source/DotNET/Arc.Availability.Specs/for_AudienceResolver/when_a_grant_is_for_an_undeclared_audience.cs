// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.for_AudienceResolver;

public class when_a_grant_is_for_an_undeclared_audience : Specification
{
    InMemoryAudienceSource _source;
    EffectiveAudiences _result;

    void Establish() => _source = new();

    IReadOnlyCollection<AudienceGrant> _observed;

    async Task Because()
    {
        await _source.Grant(AudienceLevel.Tenant, "t1", "Ghost");
        _result = await new AudienceResolver(
            _source,
            new DeclaredAudiences("Plus"),
            new AvailabilityOptions(),
            new RecordingObserver(_ => _observed = _)).Resolve(new AudienceSubject("t1", null));
    }

    [Fact] void should_never_match() => _result.Holds("Ghost").ShouldBeFalse();
    [Fact] void should_carry_the_grant() => _result.Undeclared.Single().Audience.Value.ShouldEqual("Ghost");
    [Fact] void should_report_it() => _observed.Single().Audience.Value.ShouldEqual("Ghost");

    sealed class RecordingObserver(Action<IReadOnlyCollection<AudienceGrant>> record) : IUndeclaredAudienceObserver
    {
        public void Observe(AudienceSubject subject, IReadOnlyCollection<AudienceGrant> undeclared) => record(undeclared);
    }
}
