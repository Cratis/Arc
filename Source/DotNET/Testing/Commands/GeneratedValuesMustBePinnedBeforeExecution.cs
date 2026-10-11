// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands;

/// <summary>
/// The exception that is thrown when generated value configuration changes after execution begins.
/// </summary>
public class GeneratedValuesMustBePinnedBeforeExecution() : Exception(
    "Generated value pins and AllowGeneration() must be configured before the first command executes. Create a new scenario to change them.");
