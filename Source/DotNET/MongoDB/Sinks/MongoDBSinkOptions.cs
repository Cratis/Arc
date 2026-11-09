// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.Sinks;

/// <summary>
/// Represents the configuration of a named MongoDB sink: a MongoDB server some read models are stored in.
/// </summary>
/// <remarks>
/// Set either <see cref="Server"/> or <see cref="ServerFile"/>, not both. A named sink connects to its server directly;
/// connection resolution and <see cref="MongoDBOptions.Database"/> stay with the default connection, and the database name
/// a read model is read from is the one the default connection resolves.
/// </remarks>
public class MongoDBSinkOptions
{
    /// <summary>
    /// Gets or sets the MongoDB connection string of the sink, for instance <c>mongodb://reporting:27017</c>.
    /// </summary>
    public string? Server { get; set; }

    /// <summary>
    /// Gets or sets the path of a file holding the <see cref="Server"/> connection string, such as a mounted Kubernetes Secret.
    /// </summary>
    /// <remarks>
    /// The file is read when the sink is first used and trimmed of surrounding whitespace.
    /// </remarks>
    public string? ServerFile { get; set; }
}
