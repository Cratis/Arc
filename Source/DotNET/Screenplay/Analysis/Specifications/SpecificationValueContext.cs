// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// What every value of one construction in a specification is read against.
/// </summary>
/// <param name="SemanticModel">The semantic model of the tree the construction lives in.</param>
/// <param name="Type">The type being constructed in the specification compilation.</param>
/// <param name="SourceType">The corresponding exact type declared by the application.</param>
/// <param name="Specification">The name of the specification, for use in diagnostics.</param>
/// <param name="Location">Where the specification lives, for use in diagnostics.</param>
/// <param name="Draft">The scenario collecting exact value evidence.</param>
/// <param name="QueryValues">Whether the additive Stage query literal syntax is admitted.</param>
internal sealed record SpecificationValueContext(
    SemanticModel SemanticModel,
    ITypeSymbol Type,
    ITypeSymbol SourceType,
    string Specification,
    string Location,
    SpecificationDraft Draft,
    bool QueryValues);
