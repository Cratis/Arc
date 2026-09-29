// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc;

/// <summary>
/// Log messages for <see cref="GeneratedMetadataRegistration"/>.
/// </summary>
internal static partial class GeneratedMetadataRegistrationLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Project assembly '{AssemblyName}' could not be loaded, so none of its generated metadata was registered and its types are not discovered")]
    internal static partial void ProjectAssemblyCouldNotBeLoaded(this ILogger<GeneratedMetadataRegistration> logger, string assemblyName, Exception error);
}
