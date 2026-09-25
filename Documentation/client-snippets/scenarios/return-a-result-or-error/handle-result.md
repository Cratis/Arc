```csharp
public async Task<Result<AuthorId, ValidationResult>> Handle(IMongoCollection<Author> authors)
{
    if (await authors.Find(author => author.Name == Name).AnyAsync())
    {
        return ValidationResult.Error("An author with that name is already registered.", [nameof(Name)]);
    }

    await authors.InsertOneAsync(new Author(Id, Name));
    return Id;
}
```
