// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_local_with_a_different_generation_strategy : a_generation_strategy
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_leave_the_inline_uuid_value_in_code(bool below_strategy_version)
    {
        var source = Source("GenerateValue.UuidV7()", "Guid")
            .Replace("AuthorId Receipt", "AuthorId? Receipt", StringComparison.Ordinal)
            .Replace("public AuthorRegistered Handle([GeneratedValue(\"receipt\")] AuthorId value) => new(Name, value);", "public AuthorRegistered Handle() { var receipt = new AuthorId(Guid.NewGuid()); return new(Name, receipt); }", StringComparison.Ordinal);
        Generate(new ScreenplayOptions { MaximumExecutableModelVersion = below_strategy_version ? SemanticVersion.V14 : null }, (Analyzed.SlicePath, source));
        Result.Source.ShouldNotContain(" generated");
        Result.Source.ShouldNotContain("receipt = receipt");
        Result.Source.ShouldContain("produces AuthorRegistered");
        if (below_strategy_version)
        {
            Result.Source.ShouldNotContain("generate uuidv7");
            Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
        }
        AssertDocument();
    }
}
