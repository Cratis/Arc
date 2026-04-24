// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.Discovery;

/// <summary>
/// Contains metadata for a discovered Command endpoint, including route information and associated documentation.
/// </summary>
/// <param name="Name">The unique name of the command.</param>
/// <param name="Namespace">The full namespace of the command type.</param>
/// <param name="Route">The HTTP route path configured for this command.</param>
/// <param name="Type">The fully qualified type name of the command.</param>
/// <param name="DocumentationSummary">The XML documentation summary of the command type.</param>
public record CommandDiscoveryMetadata(string Name, string Namespace, string Route, string Type, string DocumentationSummary);