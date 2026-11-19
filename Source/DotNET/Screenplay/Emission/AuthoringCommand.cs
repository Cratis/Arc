// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission;

/// <summary>
/// One command whose authoring is being resolved.
/// </summary>
/// <param name="Command">The command.</param>
/// <param name="Authoring">The authoring it declares.</param>
/// <param name="UnavailableReads">The aliases of the reads that cannot be read.</param>
internal sealed record AuthoringCommand(CommandModel Command, CommandAuthoringModel Authoring, HashSet<string> UnavailableReads);
