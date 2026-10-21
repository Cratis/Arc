// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore.for_DbSetExtensions.Model;

public class UpsertItem
{
    public string Tenant { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public UpsertValue Value { get; set; } = new(string.Empty);
    public string? Optional { get; set; }
}
