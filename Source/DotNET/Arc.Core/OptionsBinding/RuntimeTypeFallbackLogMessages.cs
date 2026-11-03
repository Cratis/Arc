// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Log messages for <see cref="RuntimeTypeFallback"/>.
/// </summary>
internal static partial class RuntimeTypeFallbackLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Option type '{TypeName}' is a subtype of an Arc option type and is bound through reflection, which is not supported when dynamic code is unavailable (trimming or NativeAOT); none of its settings, including inherited Arc settings, are guaranteed to bind")]
    internal static partial void RuntimeSubtypeBoundWithoutDynamicCode(this ILogger logger, string typeName);
}
