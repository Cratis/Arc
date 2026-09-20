# Introspection Queries Endpoint

The queries introspection endpoint returns metadata for discovered query performers, not the final runtime route table.

## Endpoint

`GET /.cratis/queries`

Normal Arc activation maps this endpoint anonymously, including in Production. Returned metadata is not filtered by the caller's query permissions. See [production access controls](index.md#production-access).

## What it returns

The endpoint returns a JSON array where each item describes one discovered query performer.

Each item includes:

- `name`: Query name.
- `namespace`: Namespace derived from the performer's location after skipping configured segments.
- `route`: Convention-derived query route using the configured prefix and skipped namespace segments.
- `fullyQualifiedName`: The performer's fully qualified query name.
- `type`: Fully qualified query type name.
- `documentationSummary`: Summary text from type metadata when available.
- `argumentsSchema`: JSON Schema for query arguments, including argument names, types, and required fields.

Current introspection does not apply a performer's custom `[Path]` or mirror all final route deduplication/replacement decisions. Treat it as discovered-operation metadata, not an authoritative inventory of callable URLs; verify custom routes against the actual mapped endpoint. For example, the [Core quickstart](../core/getting-started.md) serves its query at `/greeting`, but introspection reports the conventional `/api/get`.

## Typical uses

- Build query metadata explorers for developers.
- Inspect conventional query route generation across features.
- Feed endpoint metadata into local API diagnostics tools.
