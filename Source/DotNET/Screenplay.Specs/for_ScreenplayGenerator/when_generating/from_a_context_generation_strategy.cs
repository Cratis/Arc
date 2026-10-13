// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_context_generation_strategy : a_generation_strategy
{
    [Theory]
    [InlineData("GenerateValue.UuidV7(context)", "Guid", "generate uuidv7")]
    [InlineData("GenerateValue.Ulid(context)", "string", "generate ulid")]
    [InlineData("GenerateValue.TypeId(\"user\", context)", "string", "generate typeid \"user\"")]
    public void should_recover_the_passed_through_context(string call, string primitive, string line)
    {
        var source = Source(call, primitive)
            .Replace("IGeneratable<AuthorId>", "IGeneratableInContext<AuthorId>", StringComparison.Ordinal)
            .Replace("AuthorId New()", "AuthorId New(GenerationContext context)", StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, source));
        AssertStrategy(line);
    }
}
