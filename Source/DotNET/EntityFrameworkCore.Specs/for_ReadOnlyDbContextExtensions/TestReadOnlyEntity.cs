// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore.for_ReadOnlyDbContextExtensions;

public class TestReadOnlyEntity
{
    public TestReadOnlyId Id { get; set; } = TestReadOnlyId.NotSet;
    public string Name { get; set; } = string.Empty;
}
