// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Describes a public readable instance property of a command, read through a generated typed getter; intended for generated code only.
/// </summary>
/// <param name="Name">The property name.</param>
/// <param name="PropertyType">The declared property type.</param>
/// <param name="Read">The statically typed property getter.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record CommandReadModelKeyProperty(string Name, Type PropertyType, Func<object, object?> Read);
