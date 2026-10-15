// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// A per-stream model cannot be bound to the command's own route.
/// </summary>
/// <param name="command">The command.</param>
/// <param name="parameter">The dependency name.</param>
/// <param name="model">The model.</param>
/// <param name="source">The declared source definition.</param>
/// <param name="stream">The declared stream.</param>
/// <param name="reason">The route mismatch.</param>
public class ReadModelStreamNotOnCommandRoute(Type command, string parameter, Type model, Type source, string stream, string reason)
    : Exception($"{command.Name} reads '{parameter}' ({model.Name}), which is scoped to {source.Name} stream '{stream}', but {command.Name} {reason}. Route {command.Name} with [EventSource<{source.Name}>(\"{stream}\")] and [EventStreamId(...)], or read {model.Name} with an explicit stream through IDecisionReads in Provide.");
