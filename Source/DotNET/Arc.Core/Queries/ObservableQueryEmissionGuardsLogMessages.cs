// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

/// <summary>
/// Log messages for <see cref="ObservableQueryEmissionGuards"/>.
/// </summary>
internal static partial class ObservableQueryEmissionGuardsLogMessages
{
    [LoggerMessage(LogLevel.Error, "Observable query emission guard '{GuardType}' failed for query '{QueryName}' - failing closed and terminating the subscription")]
    internal static partial void EmissionGuardFailed(this ILogger<ObservableQueryEmissionGuards> logger, FullyQualifiedQueryName queryName, Type guardType, Exception error);
}
