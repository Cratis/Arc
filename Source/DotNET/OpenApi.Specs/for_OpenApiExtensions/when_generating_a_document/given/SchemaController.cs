// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.OpenApi.for_OpenApiExtensions.when_generating_a_document.given;

[ApiController]
public class SchemaController : ControllerBase
{
    [HttpGet("/controller")]
    public IEnumerable<with_explicit_arc_converters.SampleConcept> Get() => [new with_explicit_arc_converters.SampleConcept(Guid.NewGuid())];
}
