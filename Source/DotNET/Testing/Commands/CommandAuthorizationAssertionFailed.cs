// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands;

/// <summary>An authorization outcome did not match the expected outcome.</summary>
/// <param name="message">The safe assertion diagnostic.</param>
public class CommandAuthorizationAssertionFailed(string message) : Exception(message);
