// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when a discoverable validator of a protected decision command takes a decision dependency.
/// </summary>
/// <param name="dependencyType">The dependency type of the protected validator.</param>
public class ProtectedValidatorDependencyUnsupported(Type dependencyType) : Exception($"Protected validator dependency '{dependencyType}' is unsupported; discoverable validators cannot run in protected decision commands (Arc#2831).");
