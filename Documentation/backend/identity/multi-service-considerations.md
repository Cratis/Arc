# Multi-Service Considerations

In a microservices architecture, you have multiple options for identity details composition:

1. Single service: Implement `IProvideIdentityDetails` directly in your main service
2. Multiple services: Let ingress or reverse proxy call multiple services and merge the results
3. Dedicated identity service: Create a service that aggregates identity information from multiple sources

Choose the approach that best fits your architecture and operational requirements.
