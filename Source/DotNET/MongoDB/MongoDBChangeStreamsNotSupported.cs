// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB;

/// <summary>
/// The exception thrown when MongoDB cannot provide change streams for observable queries.
/// </summary>
/// <param name="innerException">The driver failure that reported the missing capability.</param>
public class MongoDBChangeStreamsNotSupported(Exception innerException)
    : Exception("MongoDB observable queries require change streams. Use a MongoDB replica set or a compatible service that supports change streams.", innerException);
