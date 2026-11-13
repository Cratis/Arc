// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Dynamic;

namespace Cratis.Arc.Contexts;

/// <summary>
/// Represents the dynamic view of <see cref="ContextValues"/>, where a value reads by name such as <c>values.Vessel</c>.
/// </summary>
/// <param name="values">The <see cref="ContextValues"/> to read from.</param>
[RequiresDynamicCode("Reading a value by name uses dynamic dispatch.")]
sealed class DynamicContextValues(ContextValues values) : DynamicObject
{
    /// <inheritdoc/>
    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        result = values.Lookup(binder.Name);
        return true;
    }
}
