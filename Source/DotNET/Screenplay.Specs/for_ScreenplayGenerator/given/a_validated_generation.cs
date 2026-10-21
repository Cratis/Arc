// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// Builds a generated concept with a separately declared default validator.
/// </summary>
public class a_validated_generation : a_generation_strategy
{
    protected static string ValidatedSource(string call, string rule, string primitive = "string") =>
        "using FluentValidation;\n" + Source(call, primitive) + $$"""

        public class AuthorIdValidator : Cratis.Arc.Validation.ConceptValidator<AuthorId>
        {
            public AuthorIdValidator() { {{rule}} }
        }
        """;
}
