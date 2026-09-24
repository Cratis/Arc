// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization;

/// <summary>
/// The exception that is thrown when a named policy or authentication scheme cannot be resolved unambiguously.
/// </summary>
/// <param name="message">The configuration error.</param>
public class InvalidAuthorizationConfiguration(string message) : Exception(message);
