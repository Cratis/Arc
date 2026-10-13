// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_single_expression_generation_factory : a_generation_strategy
{
    [Theory]
    [InlineData("public static AuthorId New() => new AuthorId(GenerateValue.Ulid());")]
    [InlineData("public static AuthorId New() { return new(GenerateValue.Ulid()); }")]
    [InlineData("public static AuthorId New() => GenerateValue.Ulid(); public static implicit operator AuthorId(string value) => new(value);")]
    [InlineData("static AuthorId IGeneratable<AuthorId>.New() => new(GenerateValue.Ulid());")]
    public void should_prove_the_same_primitive_forwarding_contract(string factory)
    {
        var source = Source("GenerateValue.Ulid()").Replace("public static AuthorId New() => new(GenerateValue.Ulid());", factory, StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, source));
        AssertStrategy("generate ulid");
    }
}
