// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// The command or event a specification's action puts together.
/// </summary>
/// <param name="Issued">The expression stating it.</param>
/// <param name="Construction">Where it is constructed.</param>
/// <param name="Command">The type constructed.</param>
internal sealed record SpecificationIssuedCommand(ExpressionSyntax Issued, HeldConstruction Construction, INamedTypeSymbol Command);
