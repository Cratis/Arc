// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Reactors;

/// <summary>
/// Carries a returned construction and its proven explicit destination.
/// </summary>
/// <param name="Creation">The event or command construction.</param>
/// <param name="Type">The constructed payload type.</param>
/// <param name="ReturnedType">The type actually returned to Chronicle.</param>
/// <param name="Destination">The explicit destination, if present.</param>
internal record ReactionEffect(BaseObjectCreationExpressionSyntax Creation, INamedTypeSymbol Type, INamedTypeSymbol ReturnedType, MappingSourceModel? Destination);
