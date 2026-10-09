// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Commands.for_CommandEventTags.when_resolving;

public class with_application_providers : given.a_command_event_tags_resolver
{
    object _command;
    ICanProvideCommandEventTags _secondProvider;

    void Establish()
    {
        _command = new object();
        _applicationProvider.GetEventTags(_command).Returns([new NamedTag("tenant", "first")]);
        _secondProvider = Substitute.For<SecondProvider>();
        _secondProvider.GetEventTags(_command).Returns([new NamedTag("tenant", "second")]);
        _types.FindMultiple<ICanProvideCommandEventTags>().Returns([typeof(FirstProvider), typeof(SecondProvider)]);
        _services.GetService(typeof(SecondProvider)).Returns(_secondProvider);
    }
    void Because() => _tags = Resolve(_command);

    [Fact] void should_union_every_provider() => _tags.ShouldEqual([new NamedTag("tenant", "first"), new NamedTag("tenant", "second")]);
    [Fact] void should_pass_the_command_to_each_provider() => _secondProvider.Received(1).GetEventTags(_command);
}
