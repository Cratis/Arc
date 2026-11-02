// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Commands;

/// <summary>Safe key-provider refusal logging, with no values or provider exceptions.</summary>
internal static partial class ReadModelKeyProvidersLogMessages
{
    /// <summary>Logs a key-provider refusal without caller data.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandType">The command type.</param>
    /// <param name="readModelType">The read-model type.</param>
    /// <param name="providerType">The provider type.</param>
    /// <param name="reason">The safe reason.</param>
    [LoggerMessage(LogLevel.Information, "Command {CommandType} read model {ReadModelType} key provider {ProviderType} refused lookup: {Reason}")]
    internal static partial void KeyResolutionRefused(this ILogger logger, Type commandType, Type readModelType, Type providerType, ReadModelKeyRefusal reason);
}
