// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.for_AudienceResolver;

public class when_the_subject_names_no_system_client_id : Specification
{
    CapturingAudienceSource _source;

    void Establish() => _source = new();

    async Task Because() =>
        await new AudienceResolver(_source, new DeclaredAudiences("Internal"), new AvailabilityOptions())
            .Resolve(new AudienceSubject(null, null, true));

    [Fact] void should_pass_an_empty_client_id_to_the_source() => _source.Subject.SystemClientId.ShouldEqual(string.Empty);
    [Fact] void should_keep_the_existing_constructor_shape() => new AudienceSubject("t", "u").IsSystem.ShouldBeFalse();
}
