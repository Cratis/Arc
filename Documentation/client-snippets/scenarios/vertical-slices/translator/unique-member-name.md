```csharp
// Members/Registration/Registration.cs (continued)
using Cratis.Chronicle.Events.Constraints;

namespace Library.Members.Registration;

public class UniqueMemberName : IConstraint
{
    public void Define(IConstraintBuilder builder) => builder
        .Unique(_ => _
            .On<MemberRegistered>(e => e.FirstName, e => e.LastName)
            .WithMessage("A member with that name is already registered"));
}
```
