```csharp
// Library.Specs/Authors/Registration/when_registering/and_author_does_not_exist.cs
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Specifications;
using Xunit;
using Library.Authors;
using Library.Authors.Registration;

namespace when_registering;

public class and_author_does_not_exist : Specification
{
    readonly CommandScenario<RegisterAuthor> _scenario = new();
    CommandResult _result = null!;

    async Task Because() =>
        _result = await _scenario.Execute(new RegisterAuthor("J.R.R.", "Tolkien"));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();

    [Fact] void should_append_one_event() => _scenario.AppendedEvents.Count.ShouldEqual(1);

    [Fact] async Task should_record_the_names_under_the_returned_identity()
    {
        var authorId = ((CommandResult<AuthorId>)_result).Response!;
        authorId.ShouldNotEqual(AuthorId.NotSet);
        await _scenario.ShouldHaveAppendedEvent<RegisterAuthor, AuthorRegistered>(
            (EventSourceId)authorId,
            @event => @event.FirstName.Value == "J.R.R." && @event.LastName.Value == "Tolkien");
    }

    void Destroy() => _scenario.Dispose();
}
```
