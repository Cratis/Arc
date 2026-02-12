# HTTP Middleware Testing

## Overview

The HTTP middleware components (`WellKnownRoutesMiddleware`, `StaticFilesMiddleware`, `FallbackMiddleware`, `TenantIdMiddleware`, and `HttpRequestPipeline`) are tested through integration tests rather than isolated unit tests.

## Why Integration Tests?

The middlewares operate on `HttpListenerContext`, `HttpListenerRequest`, and `HttpListenerResponse` from `System.Net`. These are sealed classes that cannot be mocked with standard mocking frameworks like NSubstitute. Attempting to create unit tests with mocks results in runtime errors from the proxy generation.

## Testing Approach

All middleware behavior is validated through the comprehensive integration tests in `for_HttpListenerEndpointMapper`. These tests:

1. **Start an actual HTTP listener** on a random port
2. **Send real HTTP requests** using `HttpClient`
3. **Verify the complete request/response cycle** through the middleware pipeline

## Test Coverage

The integration tests cover:

- **Well-known routes** - Command and query endpoint routing (`when_serving_static_files`, etc.)
- **Static file serving** - File resolution, directory traversal protection, default files
- **Fallback routing** - SPA fallback file serving for client-side routing
- **Tenant ID extraction** - Validated indirectly through route handling
- **Pipeline coordination** - 404 handling when no middleware processes the request

## Benefits of Integration Testing

1. **Real-world validation** - Tests actual HTTP behavior, not mock interactions
2. **Full stack coverage** - Validates the entire middleware pipeline working together
3. **Confidence in deployment** - Catches integration issues that unit tests might miss
4. **Simpler maintenance** - No complex mock setups to maintain

## Adding New Middleware

When adding new middleware:

1. Implement the `IHttpRequestMiddleware` interface
2. Add the middleware to the pipeline in `HttpListenerEndpointMapper.Start()`
3. Add integration tests in `for_HttpListenerEndpointMapper` to verify the new behavior

## Running Tests

```bash
dotnet test --filter "FullyQualifiedName~for_HttpListenerEndpointMapper"
```

All middleware integration tests are in: `Source/DotNET/Arc.Core.Specs/Http/for_HttpListenerEndpointMapper/`
