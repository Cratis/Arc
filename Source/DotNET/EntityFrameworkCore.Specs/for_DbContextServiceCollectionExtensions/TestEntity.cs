// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore.for_DbContextServiceCollectionExtensions;

public class TestEntity
{
    public TestId Id { get; set; } = TestId.NotSet;
    public string Name { get; set; } = string.Empty;
}
