// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

/// <summary>
/// Log messages for <see cref="QueryActionFilter"/>.
/// </summary>
internal static partial class QueryActionFilterLogMessages
{
    [LoggerMessage(LogLevel.Trace, "Controller {Controller} with action {Action} returns a regular object")]
    internal static partial void NonClientObservableReturnValue(this ILogger<QueryActionFilter> logger, string controller, string action);
}
