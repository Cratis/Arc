// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.ModelBound;

/// <summary>
/// Marks a model-bound command for protocol adapters and in-process execution only.
/// </summary>
/// <remarks>
/// The command remains discoverable and executes through the normal command pipeline, but is
/// excluded from generated HTTP execution and validation endpoints, introspection, and TypeScript
/// proxies. This attribute is not an authorization bypass or an endpoint group selector. It applies
/// only to the decorated command type, not derived types.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ProtocolOnlyAttribute : Attribute;
