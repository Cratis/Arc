# Read Models

Read models are projections of events that provide queryable views of your data. Arc provides seamless integration with Chronicle's read models, offering automatic dependency injection based on the event source ID from command context.

## What are Read Models?

In event sourcing, read models are materialized views created by projecting events. They provide optimized, denormalized data structures for querying, separate from the write model (aggregates). Read models are:

- **Eventually consistent** - Updated asynchronously as events are processed
- **Query optimized** - Structured for efficient reads
- **Projection-based** - Built from event streams
- **Cached** - Can be cached for better performance

## Key Features

- **Automatic Registration**: Read models are automatically discovered and registered in the dependency injection container
- **Event Source ID Resolution**: Automatic resolution of read model instances based on the event source ID from command context
- **Projection Integration**: Seamlessly integrated with Chronicle's projection system
- **Transient Lifecycle**: Each request gets the current state tied to a specific event source ID

## Topics

- [Read Models](read-models.md) - How to use read models with dependency injection and projections
