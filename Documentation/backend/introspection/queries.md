# Introspection Queries Endpoint

The queries introspection endpoint returns metadata for every mapped query.

## Endpoint

`GET /.cratis/queries`

## What it returns

The endpoint returns a JSON array where each item describes one query endpoint.

Each item includes:

- `name`: Query name.
- `namespace`: Namespace derived from the mapped location.
- `route`: The resolved query route.
- `type`: Fully qualified query type name.
- `documentationSummary`: Summary text from type metadata when available.

## Typical uses

- Build runtime query explorers for developers.
- Validate query route generation across features.
- Feed endpoint metadata into local API diagnostics tools.
