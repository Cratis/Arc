// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_an_executable_model_cap;

public class a_non_default_generation_strategy : a_generation_strategy
{
    [Theory]
    [InlineData("GenerateValue.Ulid()", "string", false)]
    [InlineData("GenerateValue.UuidV7()", "Guid", false)]
    [InlineData("GenerateValue.Ulid()", "string", true)]
    public void should_keep_generation_in_code_below_v15(string call, string primitive, bool authoring)
    {
        Generate(new ScreenplayOptions { MaximumExecutableModelVersion = SemanticVersion.V14, AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, Source(call, primitive).Replace("AuthorId Receipt", "AuthorId? Receipt", StringComparison.Ordinal)));
        Result.Source.ShouldNotContain("generate ");
        Result.Source.ShouldNotContain(" generated");
        var diagnostic = Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable);
        diagnostic.Message.ShouldContain("ESM v15");
        diagnostic.Location.ShouldContain(Analyzed.SlicePath);
        AssertDocument();
    }
}
