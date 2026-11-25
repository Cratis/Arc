// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_uppercase_uuid_generation : a_generation_strategy
{
    [Theory]
    [InlineData("GenerateValue.Uuid().ToString().ToUpperInvariant()", "generate uuid case upper")]
    [InlineData("GenerateValue.Uuid().ToString(\"D\").ToUpperInvariant()", "generate uuid case upper")]
    [InlineData("GenerateValue.UuidV7().ToString(\"D\").ToUpperInvariant()", "generate uuidv7 case upper")]
    public void should_recover_the_declared_strategy(string call, string line)
    {
        Generate((Analyzed.SlicePath, Source(call)));
        AssertStrategy(line);
    }
}
