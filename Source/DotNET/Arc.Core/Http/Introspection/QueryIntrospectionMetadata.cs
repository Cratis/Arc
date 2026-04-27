// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.Introspection;

/// <summary>
/// Contains metadata for an introspected Query endpoint, including route information and associated documentation.
/// </summary>
/// <param name="Name">The unique name of the query.</param>
/// <param name="Namespace">The full namespace of the query type.</param>
/// <param name="Route">The HTTP route path configured for this query.</param>
/// <param name="Type">The fully qualified type name of the query.</param>
/// <param name="DocumentationSummary">The XML documentation summary of the query type.</param>
public record QueryIntrospectionMetadata(string Name, string Namespace, string Route, string Type, string DocumentationSummary);