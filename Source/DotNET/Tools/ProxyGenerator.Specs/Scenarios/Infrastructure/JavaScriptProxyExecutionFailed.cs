// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// The exception that is thrown when a JavaScript proxy execution fails.
/// </summary>
/// <param name="message">The error message.</param>
public class JavaScriptProxyExecutionFailed(string message) : Exception(message);
