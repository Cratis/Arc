// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_InlineGeneratedValueAnalyzer.given;

public class an_inline_generation_analyzer
{
    protected const string Types = @"
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Concepts;
        public record CartId(Guid Value) : ConceptAs<Guid>(Value), IGeneratable<CartId>
        {
            public static CartId New() => new(GenerateValue.Uuid());
            public static implicit operator CartId(Guid value) => new(value);
        }
        ";
}
