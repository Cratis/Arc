// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Commands.Filters;

/// <summary>
/// Log messages for <see cref="FluentValidationFilter"/>.
/// </summary>
internal static partial class FluentValidationFilterLogMessages
{
    [LoggerMessage(LogLevel.Warning, "The validator for '{ModelType}' threw while validating; surfacing the command as invalid")]
    internal static partial void CommandValidatorThrew(this ILogger logger, string modelType, Exception exception);
}
