# Queries

Cratis Arc provides comprehensive TypeScript/JavaScript support for queries, enabling seamless integration between your frontend and backend through type-safe, automatically generated proxies. Queries retrieve data from your backend and are executed as HTTP GET operations against your backend controllers.

## Overview

The frontend query system provides:

- **Type-safe interfaces** for queries
- **HTTP integration** using the Fetch API
- **Automatic parameter validation** for queries
- **Configuration flexibility** for different environments
- **Microservice support** for distributed architectures
- **Built-in sorting and paging** support
- **Request cancellation** for improved performance

## IQuery Interface

The base query interface provides common functionality:

```typescript
interface IQuery {
    sorting: Sorting;
    paging: Paging;
}
```

## IQueryFor Interface

The specific query interface adds execution capabilities:

```typescript
interface IQueryFor<TDataType, TParameters = object> extends IQuery {
    readonly route: string;
    defaultValue: TDataType;
    parameters: TParameters | undefined;
    perform(args?: TParameters): Promise<QueryResult<TDataType>>;
}
```

## Key Features

### Client-Side Validation

Queries automatically validate parameters before executing server calls. Validation rules are defined on the backend using FluentValidation and automatically extracted by the ProxyGenerator:

```typescript
const query = new SearchUsersQuery();
query.parameters = { searchTerm: 'ab', minAge: -5 };  // Invalid

const result = await query.perform();
// Validation runs client-side before server call
// result.isValid === false
// result.validationResults contains error details
```

For more information about validation, see [Validation](./validation/index.md).

### Parameter Validation

Queries automatically validate required parameters before execution, preventing unnecessary network requests for incomplete data.

### Sorting and Paging

Built-in support for:

- **Sorting**: Field-based sorting with ascending/descending direction
- **Paging**: Page number and page size management

### Request Cancellation

Queries support automatic request cancellation when new requests are made, preventing race conditions and unnecessary processing.

## Integration with Backend

The frontend query system is designed to work seamlessly with the backend through:

### Controller-Based Queries

Backend queries are implemented as controller actions that handle HTTP GET endpoints to retrieve data.

For detailed information about implementing backend queries, see [Backend Queries](../../backend/queries/index.md).

### Automatic Proxy Generation

The most powerful feature of this system is the automatic generation of TypeScript proxies from your backend controllers. This eliminates the need for:

- Manual HTTP client code
- Type definitions that can become out of sync
- Consulting API documentation for parameter requirements

**Key Benefits:**

- **Compile-time type safety**: Catch integration errors at build time
- **IntelliSense support**: Get autocomplete and parameter hints in your IDE
- **Automatic updates**: Proxies regenerate when backend changes
- **Zero maintenance**: No manual synchronization between frontend and backend

For comprehensive information about setting up and configuring proxy generation, see [Proxy Generation](../../backend/proxy-generation/).

## Configuration

Queries support configuration for different deployment scenarios:

### Microservice Configuration

```typescript
query.setMicroservice('inventory-service');
```

### API Base Path Configuration

```typescript
query.setApiBasePath('/api/v1');
```

## Error Handling

The system provides comprehensive error handling for queries:

### Query Errors

- **Parameter validation**: Client-side validation before request
- **Network failures**: Automatic fallback to default values
- **Timeout handling**: Request cancellation and retry logic

## Best Practices

### Query Usage

1. **Set default values** to prevent undefined states
2. **Use parameters** consistently for reusable queries
3. **Implement proper loading states** during execution
4. **Handle empty results** appropriately

### Performance Considerations

1. **Leverage automatic request cancellation** for queries
2. **Implement proper error boundaries** for network failures
3. **Cache query results** when appropriate

## Next Steps

- Learn about [React query integration](../react/queries.md) for React-specific query patterns
- Explore [Commands](./commands.md) for state modification operations
- Understand [MVVM patterns](../react.mvvm/index.md) for more sophisticated frontend architectures
- Set up [Proxy Generation](../../backend/proxy-generation/) to automatically generate your query proxies
