```csharp
// Authors/Registration/Registration.cs (continued)
using Cratis.Chronicle.Events.Constraints;

namespace Library.Authors.Registration;

public class UniqueAuthorName : IConstraint
{
    public void Define(IConstraintBuilder builder) => builder
        .Unique(_ => _
            .On<AuthorRegistered>(e => e.FirstName, e => e.LastName)
            .WithMessage("An author with that name is already registered"));
}
```
