```csharp
// Members/HRIntegration/HRIntegration.cs
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Library.Members.Registration;

namespace Library.Members.HRIntegration;

// ─── External Event ───────────────────────────────────────────────────────────
// The inbound adapter records this integration event in Chronicle.
// Its string fields mirror the HR payload, not Library domain concepts.

/// <summary>Records the staff-creation payload received from HR.</summary>
[EventType]
public record HRMemberCreated(
    string EmployeeId,
    string GivenName,
    string FamilyName,
    string Status);

// ─── Translator Reactor ───────────────────────────────────────────────────────

public class MemberImportReactor(ICommandPipeline commandPipeline) : IReactor
{
    [OnceOnly]
    public async Task HandleHRMemberCreated(HRMemberCreated @event)
    {
        // Only import active staff as library members
        if (@event.Status != "ACTIVE")
        {
            return;
        }

        var result = await commandPipeline.Execute(new RegisterMember(
            FirstName: new MemberName(@event.GivenName),
            LastName: new MemberName(@event.FamilyName)));
        if (!result.IsSuccess)
        {
            throw new MemberImportFailed();
        }
    }
}

public class MemberImportFailed() : Exception("Library member registration failed.");
```
