// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandEventTags.given;

public class a_command_event_tags_resolver : Specification
{
    protected ICanProvideCommandEventTags _applicationProvider;
    protected IServiceProvider _services;
    protected ITypes _types;
    protected IEnumerable<NamedTag> _tags;

    void Establish()
    {
        _applicationProvider = Substitute.For<FirstProvider>();
        _applicationProvider.GetEventTags(Arg.Any<object>()).Returns([]);
        _services = Substitute.For<IServiceProvider>();
        _types = Substitute.For<ITypes>();
        _types.FindMultiple<ICanProvideCommandEventTags>().Returns([typeof(FirstProvider)]);
        _services.GetService(typeof(ITypes)).Returns(_types);
        _services.GetService(typeof(FirstProvider)).Returns(_applicationProvider);
    }

    protected CommandContext ContextFor(object command) => new(CorrelationId.New(), command.GetType(), command, [], [], ServiceProvider: _services);
    protected IEnumerable<NamedTag> Resolve(object command) => ContextFor(command).ResolveEventTags();

    public abstract class FirstProvider : ICanProvideCommandEventTags
    {
        public abstract IEnumerable<NamedTag> GetEventTags(object command);
    }

    public abstract class SecondProvider : ICanProvideCommandEventTags
    {
        public abstract IEnumerable<NamedTag> GetEventTags(object command);
    }
}
