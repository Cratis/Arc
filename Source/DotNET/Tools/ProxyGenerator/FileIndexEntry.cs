// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Represents an entry in the file index hierarchy.
/// </summary>
public class FileIndexEntry
{
    /// <summary>
    /// Gets or sets a value indicating whether this entry is a file.
    /// </summary>
    public bool IsFile { get; set; }

    /// <summary>
    /// Gets or sets the child entries. Null for files.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, FileIndexEntry>? Children { get; set; }
}
