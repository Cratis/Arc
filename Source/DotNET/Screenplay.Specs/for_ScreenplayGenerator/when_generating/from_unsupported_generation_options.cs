// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_unsupported_generation_options : a_generation_strategy
{
    [Theory]
    [InlineData("\"cus_\" + GenerateValue.Ulid() + \"_suffix\"")]
    [InlineData("string.Empty + GenerateValue.Ulid()")]
    [InlineData("$\"cus_{GenerateValue.Ulid()}\"")]
    [InlineData("GenerateValue.Ulid().ToLower()")]
    [InlineData("GenerateValue.Ulid().ToUpper()")]
    [InlineData("(\"CUS_\" + GenerateValue.Ulid()).ToLowerInvariant()")]
    [InlineData("GenerateValue.Integer(0, 10).ToString().ToUpperInvariant()")]
    [InlineData("System.Security.Cryptography.RandomNumberGenerator.GetHexString(32).ToUpperInvariant()")]
    [InlineData("GenerateValue.TypeId(\"cus\").ToUpperInvariant()")]
    [InlineData("\"cus_\" + GenerateValue.TypeId(\"order\")")]
    [InlineData("GenerateValue.NanoId().ToLowerInvariant()")]
    [InlineData("GenerateValue.Bytes(16).ToUpperInvariant()")]
    [InlineData("GenerateValue.Ulid().ToLowerInvariant().ToUpperInvariant()")]
    [InlineData("GenerateValue.Uuid().ToString(\"N\").ToUpperInvariant()")]
    public void should_not_publish_a_builtin_option_contract(string call)
    {
        Generate((Analyzed.SlicePath, Source(call)));
        Result.Source.ShouldNotContain("generate ulid");
        Result.Source.ShouldNotContain("generate uuid");
        Result.Source.ShouldNotContain("generate bytes");
        Result.Source.ShouldNotContain("generate nanoid");
        Result.Source.ShouldNotContain("generate typeid");
        AssertPartialDocument();
    }

    [Theory]
    [InlineData("123456789012345678901234567890123")]
    [InlineData("cüs_")]
    [InlineData("cus_\\n")]
    public void should_withhold_an_invalid_literal_prefix(string prefix)
    {
        Generate((Analyzed.SlicePath, Source($"\"{prefix}\" + GenerateValue.Ulid()")));
        Result.Source.ShouldNotContain("generate ulid");
        Result.Source.ShouldNotContain("generate rule");
        Result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable);
        AssertPartialDocument();
    }

    [Fact]
    public void should_not_apply_a_prefix_to_a_non_string_backing()
    {
        Generate((Analyzed.SlicePath, Source("Guid.Parse(\"ref_\" + GenerateValue.Uuid().ToString(\"D\"))", "Guid")));
        Result.Source.ShouldNotContain("generate uuid");
        AssertPartialDocument();
    }
}
