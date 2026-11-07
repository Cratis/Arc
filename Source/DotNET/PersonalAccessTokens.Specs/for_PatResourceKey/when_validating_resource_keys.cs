// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens.for_PatResourceKey;

public class when_validating_resource_keys : Specification
{
    [Theory]
    [InlineData("direct")]
    [InlineData("studio")]
    [InlineData("stage-a")]
    [InlineData("stage-0")]
    [InlineData("stage-app-42")]
    [InlineData("stage-app--42")]
    void should_accept_only_canonical_registered_key_shapes(string value) => new PatResourceKey(value).Value.ShouldEqual(value);

    [Fact]
    void should_accept_a_48_character_app_key() => new PatResourceKey("stage-" + new string('a', 48)).Value.Length.ShouldEqual(54);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    [InlineData("Direct")]
    [InlineData("stage-")]
    [InlineData("stage--a")]
    [InlineData("stage-a-")]
    [InlineData("stage-App")]
    [InlineData("stage-app.key")]
    [InlineData("stage-app_key")]
    [InlineData("stage-äpp")]
    [InlineData("direct ")]
    [InlineData("direct\n")]
    void should_refuse_invalid_keys_without_echoing_them(string? value)
    {
        var exception = Catch.Exception(() => _ = new PatResourceKey(value!));
        exception.ShouldBeOfExactType<ArgumentException>();
        exception!.Message.ShouldEqual("Invalid PAT resource key. (Parameter 'value')");
    }

    [Fact]
    void should_refuse_an_app_key_longer_than_48_characters() => Catch.Exception(() => _ = new PatResourceKey("stage-" + new string('a', 49))).ShouldBeOfExactType<ArgumentException>();
}
