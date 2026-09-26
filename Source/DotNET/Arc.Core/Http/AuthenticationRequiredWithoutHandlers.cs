// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http;

/// <summary>
/// The exception that is thrown when an endpoint requires authentication but no authentication handlers are registered.
/// </summary>
/// <param name="endpointName">The name of the endpoint requiring authentication.</param>
public sealed class AuthenticationRequiredWithoutHandlers(string endpointName) : Exception($"Endpoint '{endpointName}' requires authentication, but no Arc.Core authentication handler is registered.");
