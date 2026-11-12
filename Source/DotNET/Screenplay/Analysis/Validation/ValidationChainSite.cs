// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Validation;

/// <summary>
/// What every call of one rule chain is read against.
/// </summary>
/// <param name="Property">The property the chain declares rules for.</param>
/// <param name="ForEach">Whether the rules were declared for each element of a collection.</param>
/// <param name="SemanticModel">The semantic model of the tree the chain lives in.</param>
/// <param name="Location">Where the validator lives, for use in diagnostics.</param>
/// <param name="Rules">The rules collected so far.</param>
/// <param name="Conditional">Whether a named predicate in the chain executes conditionally.</param>
internal sealed record ValidationChainSite(
    string Property,
    bool ForEach,
    SemanticModel SemanticModel,
    string Location,
    IList<ValidationRuleModel> Rules,
    bool Conditional);
