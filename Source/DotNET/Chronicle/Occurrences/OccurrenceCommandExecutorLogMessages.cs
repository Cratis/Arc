// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.Occurrences;

/// <summary>
/// Log messages for <see cref="OccurrenceCommandExecutor"/>.
/// </summary>
internal static partial class OccurrenceCommandExecutorLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Effect '{EffectKey}' already has a receipt; returning the stored outcome without running command '{CommandType}'")]
    internal static partial void ReturningStoredReceipt(this ILogger logger, string effectKey, string commandType);

    [LoggerMessage(LogLevel.Information, "Command '{CommandType}' for effect '{EffectKey}' was refused and the refusal was recorded as the terminal outcome")]
    internal static partial void RefusalRecorded(this ILogger logger, string effectKey, string commandType);

    [LoggerMessage(LogLevel.Warning, "Command '{CommandType}' for effect '{EffectKey}' failed without a receipt and can be retried")]
    internal static partial void AttemptFailed(this ILogger logger, string effectKey, string commandType);
}
