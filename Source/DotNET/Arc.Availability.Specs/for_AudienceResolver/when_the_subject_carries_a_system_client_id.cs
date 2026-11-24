// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.for_AudienceResolver;

public class when_the_subject_carries_a_system_client_id : Specification
{
    CapturingAudienceSource _source;
    AudienceSubject _subject;

    void Establish()
    {
        _source = new();
        _subject = new AudienceSubject(null, null, true) { SystemClientId = "billing" };
    }

    async Task Because() =>
        await new AudienceResolver(_source, new DeclaredAudiences("Internal"), new AvailabilityOptions()).Resolve(_subject);

    [Fact] void should_pass_the_subject_to_the_source_unchanged() => _source.Subject.ShouldEqual(_subject);
    [Fact] void should_pass_the_client_id_to_the_source() => _source.Subject.SystemClientId.ShouldEqual("billing");
}
