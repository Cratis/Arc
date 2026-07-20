// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Validation;

/// <summary>
/// Log messages for <see cref="ValidatorInvoker"/>.
/// </summary>
internal static partial class ValidatorInvokerLogMessages
{
    [LoggerMessage(LogLevel.Warning, "The validator for '{ModelType}' threw while validating; surfacing the model as invalid")]
    internal static partial void ValidatorThrew(this ILogger logger, string modelType, Exception exception);
}
