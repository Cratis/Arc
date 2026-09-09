# Introspection Commands Endpoint

The commands introspection endpoint returns metadata for discovered command handlers, not the final runtime route table.

## Endpoint

`GET /.cratis/commands`

Normal Arc activation maps this endpoint anonymously, including in Production. Returned metadata is not filtered by the caller's command permissions. See [production access controls](index.md#production-access).

## What it returns

The endpoint returns a JSON array where each item describes one discovered command handler.

Each item includes:

- `name`: Command type name.
- `namespace`: Namespace derived from the handler's location after skipping configured segments.
- `route`: Convention-derived command route using the configured prefix and skipped namespace segments.
- `type`: Fully qualified command type name.
- `documentationSummary`: Summary text from type metadata when available.
- `payloadSchema`: JSON Schema describing the command payload contract (fields/properties and types).

Introspection does not mirror all final endpoint replacement decisions. Use it to inspect discovered-operation metadata, not as an authoritative inventory of callable URLs.

## Typical uses

- Generate dynamic command catalogs in internal tooling.
- Verify command route conventions in development environments.
- Support diagnostics for endpoint mapping and registration.
