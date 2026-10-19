// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// The exception that is thrown when the HTTP connection resolver authentication is missing a required setting.
/// </summary>
/// <param name="type">The configured <see cref="HttpConnectionResolverAuthenticationType"/>.</param>
/// <param name="setting">The name of the missing setting.</param>
public class HttpConnectionResolverAuthenticationNotConfigured(HttpConnectionResolverAuthenticationType type, string setting)
    : Exception($"{type} authentication for the HTTP connection resolver requires '{setting}' in the '{nameof(HttpConnectionResolverOptions.Authentication)}' options");
