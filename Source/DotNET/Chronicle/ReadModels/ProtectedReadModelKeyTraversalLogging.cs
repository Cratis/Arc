// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.ReadModels;

internal static partial class ProtectedReadModelKeyTraversalLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Protected read-model key type {KeyType} uses reflection compatibility. Register all composite members with ProtectedReadModelKeyAccessors.Register<T>(); ProtectedReadModelKeyOptions.Strict was explicitly set to false, which permits this compatibility path. No key values are logged.")]
    internal static partial void ReflectionFallback(this ILogger<ProtectedReadModelKeyTraversal> logger, string keyType);
}
