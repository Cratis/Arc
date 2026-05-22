// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Traces;

namespace Cratis.Arc.Commands;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class CommandPipelineTraces
{
    [Span("arc.command.execute", ActivityKind.Internal)]
    internal static partial IActivityScope<CommandPipeline> Execute(this IActivitySource<CommandPipeline> source, string commandType);

    [Span("arc.command.validate", ActivityKind.Internal)]
    internal static partial IActivityScope<CommandPipeline> Validate(this IActivitySource<CommandPipeline> source, string commandType);
}
