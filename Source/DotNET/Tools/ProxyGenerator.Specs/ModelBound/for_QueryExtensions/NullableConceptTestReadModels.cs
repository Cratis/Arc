// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;
using Cratis.Arc.Queries;
using FluentValidation;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions;

public class ReadModelWithNullableConcept
{
    public string Name { get; set; } = string.Empty;

    public static IEnumerable<ReadModelWithNullableConcept> FindNullableConcept(
        OptionalName? optional,
        OptionalName required,
        [Required] OptionalName? annotated,
        ConditionalName conditional,
        LimitedName? limited,
        OptionalName defaulted = null!) => [];
}

public class ReadModelWithNullableConceptAndParameters
{
    public string Name { get; set; } = string.Empty;

    public static IEnumerable<ReadModelWithNullableConceptAndParameters> FindNullableConceptArguments(
        OptionalName? optional,
        LimitedName? explicitName,
        LimitedName? limited,
        OptionalName required,
        OptionalName? annotated,
        MaximumLengthName maxOnly,
        MaximumLengthName? nullableMaxOnly) => [];
}

public record FindNullableConceptArgumentsParameters(
    OptionalName Optional,
    LimitedName ExplicitName,
    LimitedName Limited,
    OptionalName? Required,
    [property: Required] OptionalName? Annotated,
    [property: Required] MaximumLengthName MaxOnly,
    [property: Required] MaximumLengthName NullableMaxOnly);

public class FindNullableConceptArgumentsParametersValidator : QueryValidator<FindNullableConceptArgumentsParameters>
{
    public FindNullableConceptArgumentsParametersValidator() => RuleFor(x => x.ExplicitName).NotNull();
}
