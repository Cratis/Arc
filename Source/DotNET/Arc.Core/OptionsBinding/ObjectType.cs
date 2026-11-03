// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// A class with a public parameterless constructor whose public properties are bound from child keys.
/// </summary>
internal sealed class ObjectType : BoundType
{
    readonly Func<object> _create;
    readonly Func<Member[]> _members;
    Member[]? _resolvedMembers;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectType"/> class.
    /// </summary>
    /// <param name="clrType">The class.</param>
    /// <param name="create">Creates an instance.</param>
    /// <param name="members">The members, in the order the reflection binder visits them. Built on first use.</param>
    internal ObjectType(Type clrType, Func<object> create, Func<Member[]> members)
        : base(clrType)
    {
        _create = create;
        _members = members;
    }

    /// <summary>
    /// Gets the names of the bound members, in binding order.
    /// </summary>
    internal IEnumerable<string> MemberNames => (_resolvedMembers ??= _members()).Select(_ => _.Name);

    /// <inheritdoc/>
    internal override object CreateInstance() => _create();

    /// <inheritdoc/>
    internal override void BindChildren(BindingPoint point, IConfiguration config)
    {
        if (point.Value is null)
        {
            if (point.IsReadOnly)
            {
                return;
            }

            point.SetValue(CreateInstance());
        }

        if (RuntimeTypeFallback.BindWhenSubtype(this, point, config))
        {
            return;
        }

        _resolvedMembers ??= _members();
        foreach (var member in _resolvedMembers)
        {
            member.Bind(point.Value!, config);
        }
    }
}
