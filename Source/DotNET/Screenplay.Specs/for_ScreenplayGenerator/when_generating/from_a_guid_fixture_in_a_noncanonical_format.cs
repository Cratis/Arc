// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_guid_fixture_in_a_noncanonical_format : a_pinned_uuid_value
{
    [Theory]
    [InlineData("AAAAAAAABBBB7CCC8DDDEEEEEEEEEEEE", "GenerateValue.UuidV7()", "aaaaaaaa-bbbb-7ccc-8ddd-eeeeeeeeeeee")]
    [InlineData("{AAAAAAAA-BBBB-7CCC-8DDD-EEEEEEEEEEEE}", "GenerateValue.UuidV7()", "aaaaaaaa-bbbb-7ccc-8ddd-eeeeeeeeeeee")]
    [InlineData("AAAAAAAA-BBBB-7CCC-8DDD-EEEEEEEEEEEE", "GenerateValue.UuidV7()", "aaaaaaaa-bbbb-7ccc-8ddd-eeeeeeeeeeee")]
    [InlineData("AAAAAAAABBBB4CCC8DDDEEEEEEEEEEEE", "GenerateValue.Uuid()", "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee")]
    [InlineData("{AAAAAAAA-BBBB-4CCC-8DDD-EEEEEEEEEEEE}", "GenerateValue.Uuid()", "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee")]
    public void should_normalize_before_checking_the_strategy_shape(string pin, string factory, string canonical)
    {
        GeneratePin(pin, new(), factory: factory);
        Result.Source.ShouldContain($"generated receipt = \"{canonical}\"");
        AssertDocument();
        Run("WhenAllocatingAndAReceiptIsPinned").Passed.ShouldBeTrue();
    }
}
