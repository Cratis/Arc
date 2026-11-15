// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Identifies the endpoint whose client validation is described.</summary>
/// <param name="Kind">Command or query.</param>
/// <param name="Name">The generated endpoint name.</param>
/// <param name="Route">The endpoint route.</param>
internal sealed record ValidationProjectionEndpoint(string Kind, string Name, string Route);
