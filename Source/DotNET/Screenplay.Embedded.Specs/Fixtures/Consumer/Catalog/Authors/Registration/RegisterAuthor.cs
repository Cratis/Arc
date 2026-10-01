// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace Company.Library.Catalog.Authors.Registration;

/// <summary>
/// Represents registering an author in the fixture catalog.
/// </summary>
/// <param name="Name">The author's name.</param>
[Command]
public record RegisterAuthor(string Name)
{
    /// <summary>
    /// Returns the fact that the author was registered.
    /// </summary>
    /// <returns>The registration event.</returns>
    public AuthorRegistered Handle() => new(Name);
}
