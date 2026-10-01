// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Company.Library.Catalog.Authors.Registration;

/// <summary>
/// Represents an author registered in the fixture catalog.
/// </summary>
/// <param name="Name">The registered author's name.</param>
[EventType]
public record AuthorRegistered(string Name);
