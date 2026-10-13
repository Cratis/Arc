// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_legacy_local_generation_strategy : a_generation_strategy
{
    [Theory]
    [InlineData("GenerateValue.UuidV7()", "Guid", "generate uuidv7")]
    [InlineData("GenerateValue.Ulid()", "string", "generate ulid")]
    public void should_recover_the_concept_owned_strategy_through_the_shared_reader(string call, string primitive, string line)
    {
        var source = Source(call, primitive).Replace(
            "public AuthorRegistered Handle([GeneratedValue(\"receipt\")] AuthorId value) => new(Name, value);",
            "public AuthorRegistered Handle() { var receipt = AuthorId.New(); return new(Name, receipt); }",
            StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, source));
        AssertStrategy(line);
    }
}
