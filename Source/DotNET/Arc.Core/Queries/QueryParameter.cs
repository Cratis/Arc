// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents a query parameter with its name and type.
/// </summary>
/// <param name="Name">The name of the query parameter.</param>
/// <param name="Type">The type of the query parameter.</param>
public record QueryParameter(string Name, Type Type);
