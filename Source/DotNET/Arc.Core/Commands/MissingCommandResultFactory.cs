// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// The exception that is thrown when a command response cannot be wrapped in a <see cref="CommandResult{TResponse}"/>
/// because no factory was generated for its runtime type and dynamic code is not supported, as under NativeAOT.
/// </summary>
/// <remarks>
/// The Arc source generator emits factories for the concrete response types a command's Handle method declares.
/// Declare the concrete response type rather than a base type, interface or object.
/// </remarks>
/// <param name="responseType">The runtime type of the response.</param>
public class MissingCommandResultFactory(Type responseType)
    : Exception($"No generated command result factory exists for response type '{responseType}', and dynamic code is not supported. Declare the concrete response type as the return type of the command's Handle method.");
