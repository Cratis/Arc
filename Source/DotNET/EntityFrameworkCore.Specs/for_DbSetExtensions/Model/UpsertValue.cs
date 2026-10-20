// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.EntityFrameworkCore.for_DbSetExtensions.Model;

public record UpsertValue(string Value) : ConceptAs<string>(Value);
