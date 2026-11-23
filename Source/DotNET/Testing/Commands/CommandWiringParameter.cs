// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Testing.Commands;

/// <summary>The metadata available to a wiring rule. Rules must not execute parameter resolvers or command code.</summary>
/// <param name="CommandType">The owning command.</param>
/// <param name="Parameter">The declared dependency.</param>
/// <param name="Services">The caller-owned scoped provider.</param>
/// <param name="IsValidatorDependency">Whether this is a validator constructor dependency.</param>
/// <param name="IsProtected">Whether the effective command policy is protected.</param>
/// <param name="ReadProtection">The effective read protection mode, including the application's operational cap.</param>
public sealed record CommandWiringParameter(Type CommandType, ParameterInfo Parameter, IServiceProvider Services, bool IsValidatorDependency, bool IsProtected, ReadProtectionMode ReadProtection);
