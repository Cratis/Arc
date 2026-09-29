```csharp
// Members/Registration/Registration.cs
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

namespace Library.Members.Registration;

/// <summary>Records a member's registration in the Library.</summary>
[EventType]
public record MemberRegistered(MemberName FirstName, MemberName LastName);

[Command]
public record RegisterMember(MemberName FirstName, MemberName LastName)
{
    public MemberId Provide() => MemberId.New();

    public (MemberId, MemberRegistered) Handle(MemberId memberId) =>
        (memberId, new MemberRegistered(FirstName, LastName));
}
```
