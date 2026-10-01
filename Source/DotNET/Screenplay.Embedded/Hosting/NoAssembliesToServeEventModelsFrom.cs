// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting;

/// <summary>
/// Exception that gets thrown when the event model explorer is mapped without anything to serve documents from.
/// </summary>
/// <remarks>
/// It happens when no assemblies are named and the host has no entry assembly, as in some test runners.
/// Mapping an explorer that could never answer is a mistake worth saying out loud at startup.
/// </remarks>
public class NoAssembliesToServeEventModelsFrom()
    : Exception("No assemblies to serve event models from - none were given, and the process has no entry assembly.");
