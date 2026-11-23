// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Arc.Exceptions;

/// <summary>Validates the intentionally narrow public response contract.</summary>
internal static class ExceptionResponseValidation
{
    /// <summary>Checks whether the response contains only supported public metadata.</summary>
    /// <param name="response">The proposed response.</param>
    /// <returns>Whether the response can be applied.</returns>
    internal static bool IsValid(ExceptionResponse response) =>
        (int)response.StatusCode is >= 400 and <= 599 &&
        response.ErrorCode is { Length: >= 1 and <= 128 } code &&
        code.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-') &&
        response.Headers.All(header =>
            string.Equals(header.Key, "Retry-After", StringComparison.OrdinalIgnoreCase) && IsRetryAfter(header.Value));

    static bool IsRetryAfter(string value) =>
        !string.IsNullOrEmpty(value) &&
        !value.Contains('\r') && !value.Contains('\n') &&
        ((value.All(char.IsAsciiDigit) && long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out _)) ||
         DateTimeOffset.TryParseExact(value, "r", CultureInfo.InvariantCulture, DateTimeStyles.None, out _));
}
