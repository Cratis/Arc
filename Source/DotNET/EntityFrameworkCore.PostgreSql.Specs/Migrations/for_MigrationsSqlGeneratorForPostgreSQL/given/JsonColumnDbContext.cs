// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.PostgreSql.Migrations.for_MigrationsSqlGeneratorForPostgreSQL.given;

public class JsonColumnDbContext(DbContextOptions<JsonColumnDbContext> options) : DbContext(options);
