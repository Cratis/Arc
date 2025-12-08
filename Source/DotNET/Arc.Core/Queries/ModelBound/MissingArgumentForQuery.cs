// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// The exception that is thrown when a null argument is provided for a non-nullable parameter.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MissingArgumentForQuery"/> class.
/// </remarks>
/// <param name="parameterName">The name of the parameter that received a null value.</param>
/// <param name="parameterType">The type of the parameter that received a null value.</param>
/// <param name="queryName">The name of the query being performed.</param>
public class MissingArgumentForQuery(string parameterName, Type parameterType, FullyQualifiedQueryName queryName)
    : Exception($"Missing argument '{parameterName}' of type '{parameterType.Name}' when performing query '{queryName}'");
