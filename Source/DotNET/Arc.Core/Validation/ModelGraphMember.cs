// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace Cratis.Arc.Validation;

/// <summary>
/// Describes a property the Arc source generator found worth walking on a model type, and how to read it.
/// </summary>
/// <remarks>
/// Not intended to be used directly; see <see cref="ModelGraphWalkers"/>.
/// </remarks>
/// <param name="Name">The property name, as declared.</param>
/// <param name="DeclaredType">The declared type of the property.</param>
/// <param name="Read">Reads the property from an instance of the type the member was registered for.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record ModelGraphMember(string Name, Type DeclaredType, Func<object, object?> Read);
