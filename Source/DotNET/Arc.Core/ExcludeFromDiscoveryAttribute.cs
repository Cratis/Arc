// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Excludes a command or query from Arc's introspection catalogs and generated API descriptions.
/// The endpoint remains mapped and executable; this attribute does not enforce authorization.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class ExcludeFromDiscoveryAttribute : Attribute;
