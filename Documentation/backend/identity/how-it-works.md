# How It Works

The identity system enriches base identity provider data during authentication and ingress composition.

The provider flow can:

1. Verify whether the user is authorized to access your application
2. Add custom properties and details specific to your domain
3. Return a consolidated identity object your application can use

This supports a single ingress request that can both authorize the user and produce all identity headers or cookies needed by clients.
