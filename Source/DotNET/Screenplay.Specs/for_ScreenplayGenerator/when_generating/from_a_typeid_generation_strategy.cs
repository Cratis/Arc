// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_typeid_generation_strategy : a_generation_strategy
{
    [Theory]
    [InlineData("user")]
    [InlineData("")]
    [InlineData("a_b")]
    public void should_recover_the_literal_prefix(string prefix)
    {
        Generate((Analyzed.SlicePath, Source($"GenerateValue.TypeId(\"{prefix}\")")));
        AssertStrategy($"generate typeid \"{prefix}\"");
    }
}
