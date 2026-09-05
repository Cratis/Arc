// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.given;

public class a_read_model_resolution : Specification
{
    protected EventSourceId _eventSourceId;
    protected Subject _subject;
    protected CommandContext _commandContext;
    protected IReadModels _readModels;
    protected TestReadModel _readModel;
    protected TestReadModel _releasedReadModel;
    protected object? _result;
    protected Exception _exception;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _subject = new Subject("customer-42");
        _commandContext = CreateCommandContext(_eventSourceId);
        _readModels = Substitute.For<IReadModels>();
    }

    protected void GivenSubject() => _commandContext = CreateCommandContext(_eventSourceId, _subject);

    protected void GivenSubjectInResponse() =>
        _commandContext = CreateCommandContext(_eventSourceId) with { Response = new Subject("customer-response") };

    protected void GivenEventSourceIdInResponse() =>
        _commandContext = CreateCommandContext(EventSourceId.Unspecified, _subject) with { Response = _eventSourceId };

    protected void GivenEventSourceIdIsUnspecified()
    {
        _eventSourceId = EventSourceId.Unspecified;
        _commandContext = CreateCommandContext(_eventSourceId);
    }

    protected void GivenReadModelExists()
    {
        _readModel = new TestReadModel("encrypted");
        _readModels.GetInstanceById(typeof(TestReadModel), _eventSourceId, default).Returns(Task.FromResult<object>(_readModel));
    }

    protected void GivenReadModelDoesNotExist() =>
        _readModels.GetInstanceById(typeof(TestReadModel), _eventSourceId, default).Returns(Task.FromResult<object>(null!));

    protected void GivenReadModelReleases()
    {
        _releasedReadModel = new TestReadModel("decrypted");
        _readModels.Release(_readModel).Returns(Task.FromResult(_releasedReadModel));
    }

    protected void ResolveReadModel() =>
        _result = Microsoft.Extensions.DependencyInjection.ReadModelServiceCollectionExtensions.ResolveReadModel(typeof(TestReadModel), _commandContext, _readModels);

    protected void CatchResolveReadModelException() =>
        _exception = Catch.Exception(ResolveReadModel);

    protected void ShouldHaveReleasedReadModel() =>
        _readModels.Received(1).Release(_readModel);

    protected void ShouldNotHaveReleasedReadModel() =>
        _readModels.DidNotReceive().Release(Arg.Any<TestReadModel>());

    protected bool HasGotReadModelFor(EventSourceId eventSourceId) =>
        _readModels.ReceivedCalls().Any(_ =>
            _.GetMethodInfo().Name == nameof(IReadModels.GetInstanceById) &&
            _.GetArguments() is [Type readModelType, ReadModelKey actualReadModelKey, _] &&
            readModelType == typeof(TestReadModel) &&
            actualReadModelKey.Value == eventSourceId.Value);

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

    protected record TestCommand;
    protected record TestReadModel(string Value);
}
