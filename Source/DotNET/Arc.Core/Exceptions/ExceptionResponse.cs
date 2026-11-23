// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Net;

namespace Cratis.Arc.Exceptions;

/// <summary>
/// Represents an intentionally public exception response, without exception detail.
/// </summary>
public sealed record ExceptionResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionResponse"/> class.
    /// </summary>
    /// <param name="statusCode">An HTTP failure status in the range 400–599.</param>
    /// <param name="errorCode">A constant public token of 1–128 ASCII letters, digits, dots, underscores or hyphens.</param>
    /// <param name="headers">Optional Retry-After, as nonnegative delta-seconds or an HTTP date. Other headers are unsupported.</param>
    public ExceptionResponse(HttpStatusCode statusCode, string errorCode, IReadOnlyDictionary<string, string>? headers = null)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Headers = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(headers ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>Gets the failure status, used only before HTTP headers are committed.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>Gets the stable public code carried by failed result payloads.</summary>
    public string ErrorCode { get; }

    /// <summary>Gets the optional HTTP retry hint, used only before headers are committed.</summary>
    public IReadOnlyDictionary<string, string> Headers { get; }
}
