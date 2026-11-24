// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

internal static class ConceptLengthSources
{
    internal static string Concepts(IEnumerable<(string Name, string Predicate, string Message)> concepts) =>
        """
        using Cratis.Arc.Validation;
        using Cratis.Concepts;
        using FluentValidation;
        namespace Library.Common;

        """ + string.Concat(concepts.Select(concept => $$"""
        public record {{concept.Name}}(string Value) : ConceptAs<string>(Value);
        public class {{concept.Name}}Validator : ConceptValidator<{{concept.Name}}>
        {
            const int Minimum = 3;
            static readonly string other = "other";
            public {{concept.Name}}Validator() => RuleFor(_ => _.Value).Must(value => {{concept.Predicate}}).WithMessage("{{concept.Message}}");
        }

        """));
}
