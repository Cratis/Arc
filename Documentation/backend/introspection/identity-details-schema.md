# Introspection Identity Details Schema Endpoint

The identity details schema introspection endpoint returns JSON Schema for the identity details contract exposed by your configured identity details provider.

## Endpoint

`GET /.cratis/identity-details/schema`

## When Arc maps this endpoint

Arc maps this endpoint when an `IProvideIdentityDetails` implementation is registered.

## What it returns

The endpoint returns a JSON Schema document generated from the runtime identity details type:

- If your provider implements `IProvideIdentityDetails<TDetails>`, Arc generates schema for `TDetails`.
- If your provider only implements non-generic `IProvideIdentityDetails`, Arc generates schema for `object`.

## Typical uses

- Discover the identity details shape at runtime without hardcoding provider types.
- Drive client-side tooling that validates or renders identity details data.
- Keep integration tests and diagnostics aligned with the active identity provider contract.
