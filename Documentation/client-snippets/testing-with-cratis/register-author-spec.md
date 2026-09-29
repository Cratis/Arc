```csharp
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Specifications;
using Xunit;

namespace Library.Authors.for_RegisterAuthor;

public class when_registering_a_new_author : Specification
{
    readonly EventSourceId _authorId = EventSourceId.New();
    readonly CommandScenario<RegisterAuthor> _scenario = new();
    CommandResult _result = default!;

    async Task Because() => _result = await _scenario.Execute(new RegisterAuthor(_authorId, "Jane Austen"));

    [Fact] void should_accept_the_command() => _result.ShouldBeSuccessful();

    [Fact] Task should_record_the_fact() =>
        _scenario.ShouldHaveAppendedEvent<RegisterAuthor, AuthorRegistered>(
            _authorId,
            e => e.Name == "Jane Austen");

    void Destroy() => _scenario.Dispose();
}
```
