// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Capabilities;

/// <summary>Logs capability admission refusals without credential contents.</summary>
internal static partial class CapabilityGateLogging
{
    /// <summary>Logs the closed refusal reason.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="refusal">The refusal reason.</param>
    [LoggerMessage(0, LogLevel.Debug, "Capability caller refused: {Refusal}")]
    internal static partial void CallerRefused(this ILogger<CapabilityGateMiddleware> logger, CallerRefusal refusal);
}
