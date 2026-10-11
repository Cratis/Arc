// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

/// <summary>
/// Represents an attributed value consumed by the fixture integration.
/// </summary>
/// <param name="Name">The project name.</param>
[ConsumedEvent]
public record ProjectRegistered(string Name);
