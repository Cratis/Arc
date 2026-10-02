// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.ProxyGenerator.ControllerBased.for_QueryExtensions;

[Route("api/nullability-contexts")]
public class NullabilityContextTestController
{
    [HttpGet("nullable")]
    public IEnumerable<string?>? NullableContext(
        [FromQuery] OptionalName? contextualOptional,
        [FromQuery] OptionalName nonNullableOverride,
        [FromQuery] OptionalName? nullableNeighbor) => [];

    [HttpGet("non-nullable")]
    public IEnumerable<string> NonNullableContext(
        [FromQuery] OptionalName contextualRequired,
        [FromQuery] OptionalName? nullableOverride,
        [FromQuery] OptionalName requiredNeighbor) => [];
}

public record NullableContextQuery(OptionalName? ContextualOptional, OptionalName NonNullableOverride, OptionalName? NullableNeighbor);

public record NonNullableContextQuery(OptionalName ContextualRequired, OptionalName? NullableOverride, OptionalName RequiredNeighbor);
