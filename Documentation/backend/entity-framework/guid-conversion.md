---
title: Guid conversion
description: Distinguish Guid value conversion from migration column declarations.
---

A .NET `Guid` and a SQL column declaration are different contracts. Arc's Guid conversion changes the value sent to SQLite; it does not tune indexes, collations, or every EF provider.

## Model conversion

`BaseDbContext` calls `ApplyGuidConversion` for relevant model types. For manual configuration, this is an `OnModelCreating` fragment with `Cratis.Arc.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore` imported:

```csharp
modelBuilder.Entity<Customer>()
    .Property(customer => customer.Id)
    .AsGuid(Database.GetDatabaseType());
```

Here `Customer.Id` is a `Guid` property and `Database` belongs to the context.

| Database | What `AsGuid(databaseType)` adds |
| --- | --- |
| SQLite | A `Guid` → string converter using `ToString("D")`, with `Guid.Parse` on reads |
| PostgreSQL | No additional conversion for a plain Guid; the EF provider supplies its mapping |
| SQL Server | No additional conversion for a plain Guid; the EF provider supplies its mapping |

For a Guid-backed concept, `AsGuid` delegates to `AsConcept`. SQLite uses string storage for that converter too. Ordinary `ConceptAs<Guid>` identifiers remain valid in standalone Arc applications.

## Migration declarations

`GuidColumn` and `AddGuidColumn` select these SQL type names independently of the model converter:

| PostgreSQL | SQL Server | SQLite |
| --- | --- | --- |
| `UUID` | `UNIQUEIDENTIFIER` | `BLOB` |

The SQLite migration helper therefore declares `BLOB` even though `AsGuid` supplies strings. Do not interpret the helper as a `CHAR(36)` declaration or as binary serialization. Review generated migrations and test existing-data compatibility before changing a schema or representation.

Arc's built-in database detection covers the three providers above. Other EF providers may have their own Guid support, but Arc does not supply MySQL/Oracle optimization or collation guarantees.

See [common column types](./common-column-types.md) and [concept conversion](./concept-as-conversion.md) for related contracts.
