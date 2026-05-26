# Frontend Integration

The identity system integrates with frontend clients through the `.cratis-identity` cookie set by the identity endpoint.

This avoids additional API calls to fetch user details after authentication. Frontend identity support reads this cookie when present and uses it as the client identity source.

For frontend usage details, see [React identity integration](../../frontend/react/identity.md).
