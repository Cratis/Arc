// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// The outcomes Arc records commands and queries with, on spans and metrics.
/// </summary>
/// <remarks>
/// Only <see cref="Error"/> marks a span as failed. The others are expected business outcomes: the caller was told
/// no, which is the application working.
/// </remarks>
public static class WellKnownOperationOutcomes
{
    /// <summary>The operation succeeded.</summary>
    public const string Success = "success";

    /// <summary>Validation rejected the operation.</summary>
    public const string Validation = "validation";

    /// <summary>Authorization denied the operation.</summary>
    public const string Authorization = "authorization";

    /// <summary>The event store rejected the append the command produced, through a constraint or a concurrency conflict.</summary>
    public const string AppendRejected = "append_rejected";

    /// <summary>The operation failed with an error.</summary>
    public const string Error = "error";
}
