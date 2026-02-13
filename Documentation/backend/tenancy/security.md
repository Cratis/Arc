# Security Considerations

- Ensure tenant ID resolution happens only after authentication.
- Enforce tenant membership checks in application services and policies.
- Log tenant access for audits and incident investigation.
- Prevent tenant ID spoofing by validating headers, claims, and parameters.
- Treat tenant ID as sensitive metadata and avoid exposing it unnecessarily.

