// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands;

/// <summary>
/// Resolves caller validation severity at the HTTP boundary.
/// </summary>
internal static class CallerValidationSeverity
{
    static readonly ConcurrentDictionary<Type, ValidationResultSeverity?> _maximums = new();

    /// <summary>
    /// Reads the caller's requested severity from HTTP headers.
    /// </summary>
    /// <param name="headers">The request headers.</param>
    /// <returns>The requested severity, if present.</returns>
    public static ValidationResultSeverity? FromHeaders(IReadOnlyDictionary<string, string> headers)
    {
        var value = headers.FirstOrDefault(header => header.Key.Equals("X-Allowed-Severity", StringComparison.OrdinalIgnoreCase)).Value;
        return int.TryParse(value, out var severity) && severity is >= 0 and <= 3
            ? (ValidationResultSeverity)severity
            : null;
    }

    /// <summary>
    /// Applies the command or host maximum to the caller's severity.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <param name="requested">The requested severity.</param>
    /// <param name="options">The host command options.</param>
    /// <returns>The capped severity, or null when absent or ignored.</returns>
    /// <exception cref="InvalidCommandValidationSeverity">The configured maximum is undefined.</exception>
    public static ValidationResultSeverity? Cap(Type commandType, ValidationResultSeverity? requested, CommandOptions options)
    {
        if (requested is null || options.IgnoreCallerAllowedSeverity)
        {
            return null;
        }

        var maximum = _maximums.GetOrAdd(commandType, MaximumFor) ?? options.MaximumCallerAllowedSeverity;
        return requested > maximum ? maximum : requested;
    }

    static ValidationResultSeverity? MaximumFor(Type commandType)
    {
        var maximum = commandType.GetCustomAttribute<AllowCallerValidationSeverityAttribute>(inherit: true)?.Maximum;
        return maximum is < ValidationResultSeverity.Unknown or > ValidationResultSeverity.Error
            ? throw new InvalidCommandValidationSeverity(commandType)
            : maximum;
    }
}
