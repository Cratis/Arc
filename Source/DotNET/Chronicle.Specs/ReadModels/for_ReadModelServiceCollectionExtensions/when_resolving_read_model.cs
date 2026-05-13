// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_resolving_read_model
{
    [Fact]
    public void should_release_read_model_with_subject_when_present()
    {
        var eventSourceId = EventSourceId.New();
        var subject = new Subject("customer-42");
        var readModel = new TestReadModel("encrypted");
        var releasedReadModel = new TestReadModel("decrypted");
        var commandContext = CreateCommandContext(eventSourceId, subject);
        var readModels = Substitute.For<IReadModels>();
        readModels.GetInstanceById(typeof(TestReadModel), eventSourceId, default).ReturnsForAnyArgs(Task.FromResult<object>(readModel));
        readModels.Release(subject, readModel).Returns(Task.FromResult(releasedReadModel));

        var result = (TestReadModel)Microsoft.Extensions.DependencyInjection.ReadModelServiceCollectionExtensions.ResolveReadModel(typeof(TestReadModel), commandContext, readModels);

        result.ShouldEqual(releasedReadModel);
        readModels.Received(1).Release(subject, readModel);
    }

    [Fact]
    public void should_release_read_model_when_subject_is_in_response()
    {
        var eventSourceId = EventSourceId.New();
        var subject = new Subject("customer-response");
        var readModel = new TestReadModel("encrypted");
        var releasedReadModel = new TestReadModel("decrypted");
        var commandContext = CreateCommandContext(eventSourceId) with { Response = subject };
        var readModels = Substitute.For<IReadModels>();
        readModels.GetInstanceById(typeof(TestReadModel), eventSourceId, default).ReturnsForAnyArgs(Task.FromResult<object>(readModel));
        readModels.Release(subject, readModel).Returns(Task.FromResult(releasedReadModel));

        var result = (TestReadModel)Microsoft.Extensions.DependencyInjection.ReadModelServiceCollectionExtensions.ResolveReadModel(typeof(TestReadModel), commandContext, readModels);

        result.ShouldEqual(releasedReadModel);
        readModels.Received(1).Release(subject, readModel);
    }

    [Fact]
    public void should_resolve_read_model_when_event_source_id_is_in_response()
    {
        var eventSourceId = EventSourceId.New();
        var subject = new Subject("customer-84");
        var readModel = new TestReadModel("encrypted");
        var releasedReadModel = new TestReadModel("decrypted");
        var commandContext = CreateCommandContext(EventSourceId.Unspecified, subject) with { Response = eventSourceId };
        var readModels = Substitute.For<IReadModels>();
        readModels.GetInstanceById(typeof(TestReadModel), eventSourceId, default).ReturnsForAnyArgs(Task.FromResult<object>(readModel));
        readModels.Release(subject, readModel).Returns(Task.FromResult(releasedReadModel));

        var result = (TestReadModel)Microsoft.Extensions.DependencyInjection.ReadModelServiceCollectionExtensions.ResolveReadModel(typeof(TestReadModel), commandContext, readModels);

        result.ShouldEqual(releasedReadModel);
    }

    [Fact]
    public void should_return_loaded_read_model_when_subject_is_missing()
    {
        var eventSourceId = EventSourceId.New();
        var readModel = new TestReadModel("loaded");
        var commandContext = CreateCommandContext(eventSourceId);
        var readModels = Substitute.For<IReadModels>();
        readModels.GetInstanceById(typeof(TestReadModel), eventSourceId, default).ReturnsForAnyArgs(Task.FromResult<object>(readModel));

        var result = (TestReadModel)Microsoft.Extensions.DependencyInjection.ReadModelServiceCollectionExtensions.ResolveReadModel(typeof(TestReadModel), commandContext, readModels);

        result.ShouldEqual(readModel);
        readModels.DidNotReceive().Release(Arg.Any<Subject>(), Arg.Any<TestReadModel>());
    }

    [Fact]
    public void should_throw_when_event_source_id_is_unspecified()
    {
        var commandContext = CreateCommandContext(EventSourceId.Unspecified);
        var readModels = Substitute.For<IReadModels>();

        var exception = Catch.Exception(() => Microsoft.Extensions.DependencyInjection.ReadModelServiceCollectionExtensions.ResolveReadModel(typeof(TestReadModel), commandContext, readModels));

        exception.ShouldBeOfExactType<UnableToResolveReadModelFromCommandContext>();
        readModels.DidNotReceive().Release(Arg.Any<Subject>(), Arg.Any<TestReadModel>());
    }

    static CommandContext CreateCommandContext(EventSourceId eventSourceId, Subject? subject = null)
    {
        var commandContextValues = new CommandContextValues
        {
            { WellKnownCommandContextKeys.EventSourceId, eventSourceId }
        };

        if (subject is not null)
        {
            commandContextValues[WellKnownCommandContextKeys.Subject] = subject;
        }

        return new CommandContext(CorrelationId.New(), typeof(TestCommand), new TestCommand(), [], commandContextValues, null);
    }

    record TestCommand;
    record TestReadModel(string Value);
}
