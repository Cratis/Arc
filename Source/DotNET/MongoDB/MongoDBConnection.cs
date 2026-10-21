// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Represents a resolved connection to a MongoDB database.
/// </summary>
/// <param name="Client">The <see cref="IMongoClient"/> to use.</param>
/// <param name="DatabaseName">The name of the database to use.</param>
[IgnoreConvention]
public record MongoDBConnection(IMongoClient Client, string DatabaseName);
