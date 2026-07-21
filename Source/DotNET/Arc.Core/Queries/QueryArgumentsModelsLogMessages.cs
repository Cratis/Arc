// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

/// <summary>
/// Log messages for <see cref="QueryArgumentsModels"/>.
/// </summary>
internal static partial class QueryArgumentsModelsLogMessages
{
    [LoggerMessage(LogLevel.Warning, "The arguments model for query '{QueryName}' could not be materialized; falling back to validating each argument on its own")]
    internal static partial void CouldNotMaterializeArgumentsModel(this ILogger logger, string queryName, Exception exception);
}
