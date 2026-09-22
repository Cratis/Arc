```csharp
public class LibraryIdentityProvider(IMongoCollection<Member> members)
    : IProvideIdentityDetails<LibraryIdentity>
{
    public async Task<IdentityDetails> Provide(IdentityProviderContext context)
    {
        // Look the user up in your own data, keyed by the provider's id.
        var member = await members.Find(m => m.Subject == context.Id.Value).FirstOrDefaultAsync();
        if (member is null)
            return new IdentityDetails(false, LibraryIdentity.None);   // /.cratis/me rejects this result

        var identity = new LibraryIdentity(member.Id, member.Role, member.Name);
        return new IdentityDetails(true, identity);
    }
}
```
