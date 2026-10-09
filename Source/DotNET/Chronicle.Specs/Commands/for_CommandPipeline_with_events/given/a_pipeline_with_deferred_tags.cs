// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_CommandPipeline_with_events.given;

public class a_pipeline_with_deferred_tags : a_command_pipeline_with_tag_handlers
{
    protected TaggedCommand _taggedCommand;
    protected ICanProvideCommandEventTags _tagProvider;
    protected CommandContextValues _builtValues;
    ServiceProvider _metadataServices;

    void Establish()
    {
        _taggedCommand = new(EventSourceId.New(), null);
        _tagProvider = Substitute.For<ApplicationTags>();
        _tagProvider.GetEventTags(Arg.Any<object>()).Returns([new NamedTag("tenant", "one")]);
        var types = Substitute.For<ITypes>();
        types.FindMultiple<ICanProvideCommandEventTags>().Returns([typeof(ApplicationTags)]);
        _serviceProvider.GetService(typeof(ITypes)).Returns(types);
        _serviceProvider.GetService(typeof(ApplicationTags)).Returns(_tagProvider);

        // Use all actual Chronicle context providers so reintroducing eager tag resolution fails these specs.
        _metadataServices = new ServiceCollection().AddLogging()
            .AddSingleton<IInstancesOf<ICanProvideCommandEventTags>>(new KnownInstancesOf<ICanProvideCommandEventTags>([_tagProvider]))
            .BuildServiceProvider();
        var providers = typeof(EventTagAttribute).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(ICommandContextValuesProvider).IsAssignableFrom(type))
            .Select(type => (ICommandContextValuesProvider)ActivatorUtilities.GetServiceOrCreateInstance(_metadataServices, type));
        var builder = new CommandContextValuesBuilder(new KnownInstancesOf<ICommandContextValuesProvider>(providers), Substitute.For<ICommandKeys>());
        _commandContextValuesBuilder.Build(Arg.Any<object>()).Returns(call => _builtValues = builder.Build(call.Arg<object>()));
        var anyHandler = Arg.Any<ICommandHandler>();
        _commandHandlerProviders.TryGetHandlerFor(_taggedCommand, out anyHandler).Returns(call =>
        {
            call[1] = _commandHandler;
            return true;
        });
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(new TestEvent("tagged"));
    }

    void Destroy() => _metadataServices.Dispose();

    [EventTag("project", nameof(ProjectId))]
    public record TaggedCommand(EventSourceId Id, string? ProjectId) : ICanProvideEventTags
    {
        public int TagReads { get; private set; }
        public IEnumerable<NamedTag> GetEventTags()
        {
            TagReads++;
            return [];
        }
    }

    public abstract class ApplicationTags : ICanProvideCommandEventTags
    {
        public abstract IEnumerable<NamedTag> GetEventTags(object command);
    }
}
