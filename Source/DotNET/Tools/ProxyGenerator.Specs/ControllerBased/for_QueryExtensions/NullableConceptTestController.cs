// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;
using Cratis.Arc.Queries;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.ProxyGenerator.ControllerBased.for_QueryExtensions;

[Route("api/nullable-concepts")]
public class NullableConceptTestController
{
    [HttpGet]
    public IEnumerable<string> Find(
        [FromQuery] OptionalName? controllerOptional,
        [FromQuery] OptionalName controllerRequired,
        [FromQuery] LimitedName? controllerLimited,
        [FromQuery] LimitedName controllerExplicit,
        [FromQuery] MaximumLengthName controllerMaxOnly,
        [FromQuery] MaximumLengthName? controllerNullableMaxOnly,
        [FromQuery] OptionalName controllerDefaulted = null!) => [];
}

public record NullableConceptQuery(
    OptionalName ControllerOptional,
    OptionalName? ControllerRequired,
    LimitedName ControllerLimited,
    LimitedName ControllerExplicit,
    [property: Required] MaximumLengthName ControllerMaxOnly,
    [property: Required] MaximumLengthName ControllerNullableMaxOnly,
    [property: Required] OptionalName ControllerDefaulted);

public class NullableConceptQueryValidator : QueryValidator<NullableConceptQuery>
{
    public NullableConceptQueryValidator() => RuleFor(x => x.ControllerExplicit).NotNull();
}
