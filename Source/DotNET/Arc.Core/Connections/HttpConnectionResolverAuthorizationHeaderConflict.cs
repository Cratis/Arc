// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// The exception that is thrown when the HTTP connection resolver has both an <c>Authorization</c> header and authentication configured.
/// </summary>
/// <param name="type">The configured <see cref="HttpConnectionResolverAuthenticationType"/>.</param>
public class HttpConnectionResolverAuthorizationHeaderConflict(HttpConnectionResolverAuthenticationType type)
    : Exception($"The HTTP connection resolver has an 'Authorization' header configured and {type} authentication. Configure one of them");
