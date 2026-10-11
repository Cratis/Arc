// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Commands;

internal static partial class GeneratedValueResolutionLogMessages
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Generated value {Name} of type {ValueType} for command {CommandType} has validation warnings or information.")]
    internal static partial void GeneratedValueWarning(this ILogger<GeneratedValueResolution> logger, string commandType, string name, string valueType);
}
