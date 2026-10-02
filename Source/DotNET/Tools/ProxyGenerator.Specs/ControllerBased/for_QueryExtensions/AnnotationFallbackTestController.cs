// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Queries;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.ProxyGenerator.ControllerBased.for_QueryExtensions;

[Route("api/annotation-fallback")]
public class AnnotationFallbackTestController
{
    [HttpGet]
    public IEnumerable<string> Find([FromQuery] string dtoValidatedName, [FromQuery, CreditCard] string annotatedCard) => [];

    [HttpGet("without-dto-rules")]
    public IEnumerable<string> WithoutDtoRules([FromQuery, Required] string fallbackName) => [];
}

public record ControllerAnnotationFallbackQuery(string DtoValidatedName, string AnnotatedCard);

public class ControllerAnnotationFallbackQueryValidator : QueryValidator<ControllerAnnotationFallbackQuery>
{
    public ControllerAnnotationFallbackQueryValidator() => RuleFor(x => x.DtoValidatedName).NotEmpty();
}

public record ControllerWithoutRulesQuery(string FallbackName);
