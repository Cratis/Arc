// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.ReadModels;

internal static partial class CommandReadModelKeysLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Type {MetadataType} uses reflection compatibility for command read-model key lookup. Enable the Arc source generator and set ReadProtection:RequireGeneratedMetadata to true to require generated metadata. No key values are logged.")]
    internal static partial void ReflectionFallback(this ILogger logger, string metadataType);
}
