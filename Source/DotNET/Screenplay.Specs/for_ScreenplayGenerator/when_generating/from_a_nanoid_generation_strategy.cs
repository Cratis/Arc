// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_nanoid_generation_strategy : a_generation_strategy
{
    [Theory]
    [InlineData("GenerateValue.NanoId()", "generate nanoid")]
    [InlineData("GenerateValue.NanoId(32)", "generate nanoid 32")]
    [InlineData("GenerateValue.NanoId(size: 64, alphabet: \"ab\")", "generate nanoid 64 alphabet \"ab\"")]
    [InlineData("GenerateValue.NanoId(alphabet: \"ab\", size: 64)", "generate nanoid 64 alphabet \"ab\"")]
    [InlineData("GenerateValue.NanoId(21, null)", "generate nanoid")]
    public void should_recover_size_and_alphabet(string call, string line)
    {
        Generate((Analyzed.SlicePath, Source(call)));
        AssertStrategy(line);
    }
}
