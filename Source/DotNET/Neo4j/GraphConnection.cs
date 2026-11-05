// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Neo4j.Driver;

namespace Cratis.Arc.Neo4j;

/// <summary>
/// Represents a resolved connection to a Neo4j graph database.
/// </summary>
/// <param name="Driver">The shared <see cref="IDriver"/> to use. Never dispose it.</param>
/// <param name="DatabaseName">The name of the database to use.</param>
[IgnoreConvention]
public record GraphConnection(IDriver Driver, string DatabaseName);
