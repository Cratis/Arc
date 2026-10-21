// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Arc.EntityFrameworkCore;

/// <summary>
/// The members Entity Framework Core requires to be preserved on an entity type, as declared on <c>DbSet&lt;TEntity&gt;</c>.
/// </summary>
internal static class EntityTypeMembers
{
    /// <summary>
    /// The member types a generic entity parameter must preserve to be passed to <c>DbSet&lt;TEntity&gt;</c>.
    /// </summary>
    internal const DynamicallyAccessedMemberTypes Required =
        DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.NonPublicConstructors |
        DynamicallyAccessedMemberTypes.PublicFields |
        DynamicallyAccessedMemberTypes.NonPublicFields |
        DynamicallyAccessedMemberTypes.PublicProperties |
        DynamicallyAccessedMemberTypes.NonPublicProperties |
        DynamicallyAccessedMemberTypes.Interfaces;
}
