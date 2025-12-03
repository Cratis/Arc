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
    /// Gets or sets the sub-folders within this entry.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, FileIndexEntry>? Folders { get; set; }

    /// <summary>
    /// Gets or sets the files within this entry.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IList<string>? Files { get; set; }
}
