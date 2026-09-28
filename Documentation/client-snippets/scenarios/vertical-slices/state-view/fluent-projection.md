```csharp
using Cratis.Chronicle.Projections;

public class AuthorProjection : IProjectionFor<Author>
{
    public void Define(IProjectionBuilderFor<Author> builder) => builder
        .From<AuthorRegistered>();
}
```
