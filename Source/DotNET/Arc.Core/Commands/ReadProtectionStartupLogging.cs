// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Commands;

internal static partial class ReadProtectionStartupLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Read protection for {Command} is lowered from {DeclaredMode} to {EffectiveMode} by ReadProtection:MaximumMode.")]
    internal static partial void ModeLowered(this ILogger<ReadProtectionStartup> logger, string command, ReadProtectionMode declaredMode, ReadProtectionMode effectiveMode);
}
