// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_prefixed_generation : a_generation_strategy
{
    [Theory]
    [InlineData("\"cus_\" + GenerateValue.Ulid()", "generate ulid prefix \"cus_\"")]
    [InlineData("\"CUS_\" + GenerateValue.Ulid().ToLowerInvariant()", "generate ulid prefix \"CUS_\" case lower")]
    [InlineData("\"ref_\" + GenerateValue.Uuid().ToString(\"D\").ToUpperInvariant()", "generate uuid prefix \"ref_\" case upper")]
    [InlineData("\"key_\" + GenerateValue.Bytes(16)", "generate bytes 16 base64url prefix \"key_\"")]
    [InlineData("\"code_\" + GenerateValue.NanoId()", "generate nanoid prefix \"code_\"")]
    [InlineData("\"code_\" + GenerateValue.NanoId(64, \"ab\")", "generate nanoid 64 alphabet \"ab\" prefix \"code_\"")]
    [InlineData("\"cus_\" + (((GenerateValue.Ulid()))).ToLowerInvariant()", "generate ulid prefix \"cus_\" case lower")]
    public void should_preserve_the_literal_prefix_without_casing_it(string call, string line)
    {
        Generate((Analyzed.SlicePath, Source(call)));
        AssertStrategy(line);
    }

    [Theory]
    [InlineData("GenerateValue.Ulid(context).ToLowerInvariant()", "generate ulid case lower")]
    [InlineData("\"cus_\" + GenerateValue.Ulid(context).ToLowerInvariant()", "generate ulid prefix \"cus_\" case lower")]
    public void should_preserve_the_captured_context(string call, string line)
    {
        var source = Source(call)
            .Replace("IGeneratable<AuthorId>", "IGeneratableInContext<AuthorId>", StringComparison.Ordinal)
            .Replace("public static AuthorId New()", "public static AuthorId New(GenerationContext context)", StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, source));
        AssertStrategy(line);
    }
}
