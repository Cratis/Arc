// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Commands;

internal static partial class CommandPipelineLogging
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Command authorization configuration failed")]
    internal static partial void AuthorizationConfigurationFailed(this ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Command authorization preparation failed")]
    internal static partial void AuthorizationPreparationFailed(this ILogger logger, Exception exception);
}
