// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// What one call of a specification's steps is read against.
/// </summary>
/// <param name="SemanticModel">The semantic model of the tree the call lives in.</param>
/// <param name="Draft">The scenario collected so far.</param>
/// <param name="Name">The name of the specification.</param>
/// <param name="Location">Where the specification lives, for use in diagnostics.</param>
internal sealed record SpecificationStepScope(SemanticModel SemanticModel, SpecificationDraft Draft, string Name, string Location);
