// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission;

/// <summary>
/// What productions depending on unavailable reads are pruned against.
/// </summary>
/// <param name="Application">The event declarations typing required payloads.</param>
/// <param name="Diagnostics">The loss reports.</param>
/// <param name="Location">The command's slice.</param>
internal sealed record ReadPruningScope(ApplicationModel Application, ScreenplayDiagnostics Diagnostics, string Location);
