// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Represents the command whose reads are being read and what is proven about it.
/// </summary>
/// <param name="Command">The command.</param>
/// <param name="Identifier">The command's proven event source key property, if any.</param>
/// <param name="Protection">The effective read protection of the command.</param>
/// <param name="Sources">The proven source paths.</param>
/// <param name="Location">The diagnostic location.</param>
internal sealed record ReadScope(INamedTypeSymbol Command, string? Identifier, int Protection, AuthoringSources Sources, string Location);
