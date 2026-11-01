// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Describes proxies with the same name in one generated TypeScript module.
/// </summary>
/// <param name="Module">The generated module path without a file extension.</param>
/// <param name="Name">The colliding proxy name.</param>
/// <param name="Descriptors">The descriptors that would generate the colliding proxies.</param>
internal sealed record ProxyNameCollision(string Module, string Name, IReadOnlyList<IDescriptor> Descriptors);
