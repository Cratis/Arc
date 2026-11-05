---
title: Map Guid values with EF Core in Cratis Arc
description: Cratis Arc converts Guid values to strings for SQLite, while its migration helpers declare column types separately. Check both when changing a schema.
---

Cratis Arc converts .NET `Guid` values to strings for SQLite and leaves plain Guid mapping to the EF provider for PostgreSQL and SQL Server. When creating or changing a schema, check both the model conversion and the migration's column declaration: Arc configures them separately.

## Model conversion

Use `BaseDbContext` to apply `ApplyGuidConversion` to its relevant model types. For a context where you configure the mapping yourself, add `AsGuid` in `OnModelCreating`. Import `Cratis.Arc.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore`:

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

For a Guid-backed concept, `AsGuid` delegates to `AsConcept`. SQLite uses string storage for that converter too. Use `ConceptAs<Guid>` when the identifier needs a domain-specific name; it does not require Chronicle.

## Migration declarations

`GuidColumn` and `AddGuidColumn` select these SQL type names independently of the model converter:

| PostgreSQL | SQL Server | SQLite |
| --- | --- | --- |
| `UUID` | `UNIQUEIDENTIFIER` | `BLOB` |

On SQLite, the migration helper declares `BLOB` while `AsGuid` supplies strings. The declaration is neither a `CHAR(36)` mapping nor an instruction to serialize Guids as binary data. Review generated migrations and check reads and writes against existing data before changing either the schema or the value representation.

Arc's built-in database detection covers the three providers above. For other EF providers, use application-owned registration and check the provider's Guid mapping. This conversion does not configure indexes or collations.

For migration helper usage, continue to [common column types](./common-column-types.md). To define domain-specific identifiers, follow [concept conversion](./concept-as-conversion.md).
