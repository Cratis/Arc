// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// The exception that is thrown when a command response cannot be wrapped in a <see cref="CommandResult{TResponse}"/>
/// because no factory was generated for its runtime type and the code is ahead-of-time compiled, as under NativeAOT.
/// </summary>
/// <remarks>
/// Thrown only when no factory is registered for the runtime type of the response and creating the result through
/// reflection is not supported, which happens in ahead-of-time compiled code such as NativeAOT. Under the JIT, including
/// apps built with PublishAot that run from dotnet run or a test host, such responses are wrapped through reflection.
/// The Arc source generator emits factories for the concrete response types a command's Handle method declares.
/// Declare the concrete response type rather than a base type, interface or object.
/// </remarks>
/// <param name="responseType">The runtime type of the response.</param>
/// <param name="innerException">The failure from creating the result through reflection.</param>
public class MissingCommandResultFactory(Type responseType, Exception innerException)
    : Exception($"No generated command result factory exists for response type '{responseType}', and the result type cannot be created at runtime. Declare the concrete response type as the return type of the command's Handle method.", innerException);
