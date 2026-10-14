// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// The Arc read path declaration, independent of the storage provider.
/// </summary>
public enum ReadConsistency
{
    /// <summary>
    /// Reads the stored model.
    /// </summary>
    Stored = 0,

    /// <summary>
    /// Requests a current model when its shape can be certified.
    /// </summary>
    Current = 1
}
