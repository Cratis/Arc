// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;

namespace Cratis.Arc.Availability.for_ContextValueResolver;

public class when_reading_a_built_in_value_and_a_member_path : Specification
{
    ContextValueOutcome _tenant;
    ContextValueOutcome _nested;
    ContextValueOutcome _property;
    ContextValueOutcome _missing;

    async Task Because()
    {
        var request = Requests.For(
            null,
            ambient: new Dictionary<string, object?>
            {
                ["tenant"] = "t1",
                ["identity"] = new Dictionary<string, object?> { ["name"] = "ada" },
                ["occurred"] = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        _tenant = await request.Resolve("$context.tenant");
        _nested = await request.Resolve("identity.name");
        _property = await request.Resolve("occurred.Year");
        _missing = await request.Resolve("identity.email.domain");
    }

    [Fact] void should_read_the_built_in_value() => _tenant.Value.ShouldEqual("t1");
    [Fact] void should_navigate_a_dictionary() => _nested.Value.ShouldEqual("ada");
    [Fact] void should_navigate_a_property() => _property.Value.ShouldEqual(2026);
    [Fact] void should_treat_a_missing_member_as_absent() => _missing.IsPresent.ShouldBeFalse();
}
