// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries;

/// <summary>
/// The exception that is thrown when query execution fails intentionally for testing.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="QueryExecutionFailed"/> class.
/// </remarks>
/// <param name="message">The error message.</param>
public class QueryExecutionFailed(string message) : Exception(message);
