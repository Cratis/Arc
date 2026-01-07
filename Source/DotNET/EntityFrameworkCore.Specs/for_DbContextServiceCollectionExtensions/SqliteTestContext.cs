// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.for_DbContextServiceCollectionExtensions;

public class SqliteTestContext(DbContextOptions<SqliteTestContext> options) : BaseDbContext(options);
