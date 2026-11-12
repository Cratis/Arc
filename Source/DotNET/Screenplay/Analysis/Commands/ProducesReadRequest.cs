// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// What one read of a command's handlers is collecting into.
/// </summary>
/// <param name="Command">The type declaring the command.</param>
/// <param name="Location">Where the command lives, for use in diagnostics.</param>
/// <param name="Produces">The productions collected so far.</param>
/// <param name="Sources">The proven authoring mapping sources.</param>
/// <param name="GeneratedIdentity">Whether the returned tuple supplies a proven generated identity.</param>
internal sealed record ProducesReadRequest(
    INamedTypeSymbol Command,
    string Location,
    List<ProducesModel> Produces,
    AuthoringSources? Sources,
    bool GeneratedIdentity);
