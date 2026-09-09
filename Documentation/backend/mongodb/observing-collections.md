# Observing Collections

The MongoDB extensions in Cratis Applications provide powerful reactive programming capabilities through collection observation. This feature allows you to create observables that automatically notify subscribers when documents in a MongoDB collection change, providing real-time updates to your application.

## Overview

Collection observation leverages MongoDB's Change Streams feature combined with Reactive Extensions (Rx.NET) to provide a seamless way to watch for changes in your data. The system automatically handles initial data loading, change detection, and notification of observers.

## Key Features

- **Real-time Updates**: Automatically receive notifications when documents change
- **Filtering Support**: Observe only documents matching specific criteria
- **Multiple Observation Types**: Observe collections, single documents, or documents by ID
- **Query Context Awareness**: Integrates with Cratis query context for paging and sorting
- **Automatic Cleanup**: Proper resource management and cleanup when observations are disposed

## Basic Collection Observation

### Observing All Documents

```csharp
public class AuthorService
{
    private readonly IMongoCollection<Author> _collection;

    public AuthorService(IMongoCollection<Author> collection)
    {
        _collection = collection;
    }

    public IObservable<IEnumerable<Author>> ObserveAllAuthors()
    {
        return _collection.Observe();
    }
}
```

### Observing with Filter (Expression)

```csharp
public IObservable<IEnumerable<Author>> ObserveActiveAuthors()
{
    return _collection.Observe(author => author.IsActive);
}
```

### Observing with Filter Definition

```csharp
public IObservable<IEnumerable<Author>> ObserveAuthorsByCategory(string category)
{
    var filter = Builders<Author>.Filter.Eq(a => a.Category, category);
    return _collection.Observe(filter);
}
```

## Single Document Observation

### Observing Single Document with Filter

```csharp
public IObservable<Author> ObserveFeaturedAuthor()
{
    return _collection.ObserveSingle(author => author.IsFeatured);
}
```

### Observing Document by ID

```csharp
public IObservable<Author> ObserveAuthorById(AuthorId authorId)
{
    return _collection.ObserveById<Author, AuthorId>(authorId);
}
```

## When the Observed Document Is Gone

`ObserveSingle()` and `ObserveById()` push a fresh document every time the underlying data changes, but there is no document to push when:

- the observed document is deleted,
- an update or replace moves the document out of the filter (it still exists, but no longer matches), or
- the initial query never found a match in the first place.

In every one of these cases the subject emits `default` — `null` for a reference type — rather than completing. Completing the observable would end the subscription; emitting `null` reports "nothing right now" while leaving the subscription open in case the document reappears, for example if it is re-inserted with the same id, or a later update moves it back into the filter.

> **Note**: A `[ReadModel]` marked `[RemovedWith<T>]` hard-deletes its backing document when the removal event fires. An active `ObserveSingle()`/`ObserveById()` subscriber sees that removal exactly as described above — "the read model was removed" is this same `null` emission, not a special case.

On the frontend, guard against it the same way you guard against "still loading" — check `result.hasData` (and `result.isReady` if you need to distinguish "no result yet" from "ready, but nothing matches"):

```tsx
const [result] = GetAccountObservable.use(accountId);

if (!result.isReady) {
    return <Spinner />;
}

if (!result.hasData) {
    return <NotFound />;
}

return <AccountDetails account={result.data} />;
```

## Advanced Usage

### With Find Options

```csharp
public IObservable<IEnumerable<Author>> ObserveRecentAuthors()
{
    var options = new FindOptions<Author>
    {
        Sort = Builders<Author>.Sort.Descending(a => a.CreatedAt),
        Limit = 10
    };

    return _collection.Observe(
        author => author.CreatedAt > DateTime.UtcNow.AddDays(-30),
        options);
}
```

### Subscribing to Changes

```csharp
public class AuthorNotificationService
{
    private readonly IDisposable _subscription;

    public AuthorNotificationService(IMongoCollection<Author> collection)
    {
        _subscription = collection
            .Observe(author => author.IsActive)
            .Subscribe(
                authors => HandleAuthorsChanged(authors),
                error => HandleError(error),
                () => HandleCompleted());
    }

    private void HandleAuthorsChanged(IEnumerable<Author> authors)
    {
        // React to changes in active authors
        Console.WriteLine($"Active authors updated: {authors.Count()} authors");
    }

    private void HandleError(Exception error)
    {
        // Handle observation errors
        Console.WriteLine($"Error observing authors: {error.Message}");
    }

    private void HandleCompleted()
    {
        // Handle observation completion
        Console.WriteLine("Author observation completed");
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
```

## Integration with Queries

The observation system integrates seamlessly with the Cratis query context, supporting paging and sorting:

```csharp
[Route("api/authors")]
public class AuthorsController : Controller
{
    private readonly IMongoCollection<Author> _collection;

    public AuthorsController(IMongoCollection<Author> collection)
    {
        _collection = collection;
    }

    [HttpGet("observe")]
    public IObservable<IEnumerable<Author>> ObserveAuthors(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc")
    {
        // Query context will be automatically applied to the observation
        return _collection.Observe(author => author.IsPublished);
    }
}
```

## Change Types Supported

The observation system monitors the following MongoDB change stream operations:

- **Insert**: New documents added to the collection
- **Update**: Existing documents modified
- **Replace**: Documents replaced entirely  
- **Delete**: Documents removed from the collection

## Performance Considerations

### Filtering Early

Always apply filters to reduce the amount of data being observed:

```csharp
// Good - filtered observation
var activeAuthors = collection.Observe(author => author.IsActive);

// Avoid - observing all then filtering in memory
var allAuthors = collection.Observe()
    .Select(authors => authors.Where(a => a.IsActive));
```

### Resource Management

Properly dispose of subscriptions to avoid memory leaks:

```csharp
public class AuthorService : IDisposable
{
    private readonly CompositeDisposable _subscriptions = new();

    public void StartObserving()
    {
        var subscription = _collection
            .Observe(author => author.IsActive)
            .Subscribe(HandleAuthorsChanged);
            
        _subscriptions.Add(subscription);
    }

    public void Dispose()
    {
        _subscriptions?.Dispose();
    }
}
```

### Batch Updates

The system automatically batches rapid successive changes to reduce notification frequency and improve performance.

## Error Handling

Robust error handling is essential when working with observables:

```csharp
public void ObserveWithErrorHandling()
{
    _collection
        .Observe(author => author.IsActive)
        .Retry(3) // Retry up to 3 times on error
        .Catch(Observable.Empty<IEnumerable<Author>>()) // Continue with empty on final failure
        .Subscribe(
            authors => HandleAuthors(authors),
            error => _logger.LogError(error, "Failed to observe authors"));
}
```

## Best Practices

### Use Specific Filters

Apply filters to observe only the data you need:

```csharp
// Good - specific filter
collection.Observe(doc => doc.Status == "Active" && doc.Type == "Premium");

// Avoid - broad observation with post-filtering
collection.Observe().Where(docs => docs.All(d => d.Status == "Active"));
```

### Dispose Properly

Always dispose subscriptions when they're no longer needed:

```csharp
public class ComponentWithObservation : IDisposable
{
    private IDisposable? _subscription;

    public void StartObserving()
    {
        _subscription = collection.Observe().Subscribe(HandleData);
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
```

### Handle Connection Issues

Implement retry logic for connection interruptions:

```csharp
collection
    .Observe(filter)
    .RetryWhen(errors => errors
        .SelectMany(error => Observable.Timer(TimeSpan.FromSeconds(5)))
        .Take(5)) // Retry 5 times with 5-second intervals
    .Subscribe(HandleData);
```

## Integration with Dependency Injection

Register observation services in your DI container:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<IAuthorObservationService, AuthorObservationService>();
}

public interface IAuthorObservationService
{
    IObservable<IEnumerable<Author>> ObserveActiveAuthors();
    IObservable<Author> ObserveAuthorById(AuthorId id);
}

public class AuthorObservationService : IAuthorObservationService
{
    private readonly IMongoCollection<Author> _collection;

    public AuthorObservationService(IMongoCollection<Author> collection)
    {
        _collection = collection;
    }

    public IObservable<IEnumerable<Author>> ObserveActiveAuthors()
    {
        return _collection.Observe(author => author.IsActive);
    }

    public IObservable<Author> ObserveAuthorById(AuthorId id)
    {
        return _collection.ObserveById<Author, AuthorId>(id);
    }
}
```

## Troubleshooting

### Common Issues

#### Change Stream Not Starting

- Ensure MongoDB version supports change streams (3.6+)
- Verify replica set configuration
- Check user permissions for change stream operations

#### Memory Leaks

- Always dispose subscriptions when no longer needed
- Use `CompositeDisposable` for managing multiple subscriptions
- Implement `IDisposable` in classes that create observations

#### Performance Issues

- Apply filters at the database level, not in memory
- Limit the scope of observations to necessary data
- Monitor change stream performance in MongoDB logs

### Debugging

Enable logging to troubleshoot observation issues:

```csharp
services.Configure<LoggerFilterOptions>(options =>
{
    options.AddFilter("MongoDB.Driver.MongoCollection", LogLevel.Debug);
});
```

This will provide detailed information about change stream operations and any issues encountered during observation.
