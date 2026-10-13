// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_bytes_generation_strategy : a_generation_strategy
{
    [Theory]
    [InlineData("GenerateValue.Bytes(32)", "generate bytes 32 base64url")]
    [InlineData("GenerateValue.Bytes(16, ValueEncoding.Base64Url)", "generate bytes 16 base64url")]
    [InlineData("GenerateValue.Bytes(16, ValueEncoding.Hex)", "generate bytes 16 hex")]
    [InlineData("GenerateValue.Bytes(16, ValueEncoding.Base32)", "generate bytes 16 base32")]
    [InlineData("GenerateValue.Bytes(encoding: ValueEncoding.Hex, count: 32)", "generate bytes 32 hex")]
    public void should_recover_encoding_without_changing_letter_case(string call, string line)
    {
        Generate((Analyzed.SlicePath, Source(call)));
        AssertStrategy(line);
    }

#if NET9_0_OR_GREATER
    [Theory]
    [InlineData("System.Security.Cryptography.RandomNumberGenerator.GetHexString(64, lowercase: true)", "generate bytes 32 hex")]
    [InlineData("System.Security.Cryptography.RandomNumberGenerator.GetHexString(32, lowercase: false)", "generate bytes 16 hex case upper")]
    [InlineData("System.Security.Cryptography.RandomNumberGenerator.GetHexString(32)", "generate bytes 16 hex case upper")]
    public void should_translate_hex_character_count_to_byte_count(string call, string line)
    {
        Generate((Analyzed.SlicePath, Source(call)));
        AssertStrategy(line);
    }
#endif
}
