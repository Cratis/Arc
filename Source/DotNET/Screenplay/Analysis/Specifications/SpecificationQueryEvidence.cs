// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Represents one exact generated query scenario - a substitute read model, the exact query it calls, the input it
/// calls it with, and the read model instance it expects back.
/// </summary>
/// <param name="SourceType">The exact authored specification type.</param>
/// <param name="Source">The exact authored specification declaration.</param>
/// <param name="Query">The exact application-declared query method matched cross-compilation.</param>
/// <param name="QueryInvocationSource">The exact authored query call location.</param>
/// <param name="ReadModel">The exact application-declared read model type the query returns.</param>
/// <param name="ExpectedSource">The exact authored expected read-model construction location.</param>
/// <param name="IsOptional">Whether the query may answer with no read model at all.</param>
/// <param name="Arguments">The exact query call arguments, in the query's own formal parameter order.</param>
/// <param name="Result">The exact expected read-model values, in its declaration order.</param>
/// <param name="ValueEvidence">Every argument and result value's exact authored expression location.</param>
internal sealed record SpecificationQueryEvidence(
    INamedTypeSymbol SourceType,
    Location Source,
    IMethodSymbol Query,
    Location QueryInvocationSource,
    INamedTypeSymbol ReadModel,
    Location ExpectedSource,
    bool IsOptional,
    IReadOnlyList<PropertyMappingModel> Arguments,
    IReadOnlyList<PropertyMappingModel> Result,
    IReadOnlyDictionary<PropertyMappingModel, Location> ValueEvidence);
