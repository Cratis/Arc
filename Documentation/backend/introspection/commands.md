# Introspection Commands Endpoint

The commands introspection endpoint returns metadata for every mapped command.

## Endpoint

`GET /.cratis/commands`

## What it returns

The endpoint returns a JSON array where each item describes one command endpoint.

Each item includes:

- `name`: Command type name.
- `namespace`: Namespace derived from the mapped location.
- `route`: The resolved command route.
- `type`: Fully qualified command type name.
- `documentationSummary`: Summary text from type metadata when available.

## Typical uses

- Generate dynamic command catalogs in internal tooling.
- Verify command route conventions in development environments.
- Support diagnostics for endpoint mapping and registration.
