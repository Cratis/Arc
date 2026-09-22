```csharp
using System.Threading.Tasks;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Specifications;
using Library.Authors;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Library.Specs;

public class when_recording_an_author : Specification
{
    readonly CommandScenario<RecordAuthor> _scenario = new();
    readonly AuthorId _id = AuthorId.New();
    readonly AuthorName _name = new("Ada Lovelace");
    IAuthorRegistration _registration = null!;
    CommandResult _result = null!;

    void Establish()
    {
        _registration = Substitute.For<IAuthorRegistration>();
        _registration.Register(_id, _name).Returns(Task.CompletedTask);
        _scenario.Services.AddSingleton(_registration);
    }

    async Task Because() => _result = await _scenario.Execute(new RecordAuthor(_id, _name));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] async Task should_register_the_author() => await _registration.Received(1).Register(_id, _name);

    void Destroy() => _scenario.Dispose();
}
```
