// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents a parameter a query takes.
/// </summary>
/// <param name="Name">The name of the parameter.</param>
/// <param name="Type">The kind of value the parameter takes.</param>
public record QueryParameter(string Name, QueryParameterType Type);
