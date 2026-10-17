// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Represents where a Uuid condition is read and how its operand paths are resolved.
/// </summary>
/// <param name="Model">The semantic model.</param>
/// <param name="Location">The diagnostic location.</param>
/// <param name="Diagnostics">The diagnostic collector.</param>
/// <param name="CommandPath">Resolves an expression to a direct command property.</param>
/// <param name="ReadPath">Resolves an expression to a protected read path, or null where none is admitted.</param>
internal readonly record struct UuidConditionSite(
    SemanticModel Model,
    string Location,
    ScreenplayDiagnostics Diagnostics,
    Func<ExpressionSyntax, string?> CommandPath,
    Func<ExpressionSyntax, string?>? ReadPath);
