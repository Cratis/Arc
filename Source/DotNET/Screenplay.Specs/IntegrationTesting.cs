// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay;

/// <summary>
/// Declares the testing surface a Chronicle integration specification is written against, as source.
/// </summary>
/// <remarks>
/// The generator recognizes these by the fully qualified name of the type declaring each member, which is exactly
/// what lets it read a specification without the testing packages being referenced. Declaring them here rather than
/// referencing them keeps every specification about reading them hermetic, and asks the recognition the only
/// question worth asking of it - whether the names alone are enough.
/// <para>
/// The shape follows the real packages: what Chronicle adds to a command scenario - <c>Given</c>, <c>EventSequence</c>
/// and <c>EventScenario</c> - are extension properties declared in <c>Cratis.Arc.Chronicle.Testing.Commands</c>, so a
/// specification reaches them only with that namespace imported, and the event scenario seeds through
/// <c>Given.ForEventSource(...).Events(...)</c> exactly as a specification written against the packages does.
/// </para>
/// </remarks>
public static class IntegrationTesting
{
    /// <summary>
    /// The path the surface is compiled as.
    /// </summary>
    public const string Path = "Library/Testing/IntegrationTesting.cs";

    /// <summary>
    /// The source of the surface.
    /// </summary>
    public const string Source = """
        using System.Net.Http;
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.EventSequences;

        namespace Cratis.Arc.Testing.Commands
        {
            public class CommandScenario<TCommand>
            {
                public Task<CommandResult> Execute(TCommand command) => Task.FromResult(new CommandResult());

                public Task<Result> Validate(TCommand command) => Task.FromResult(new Result());
            }

            public class Result
            {
                public bool IsSuccess => true;
            }

            public class CommandResult : Result;

        }

        namespace Cratis.Arc.Chronicle.Testing.Commands
        {
            public static class CommandScenarioChronicleExtensions
            {
                extension<TCommand>(Cratis.Arc.Testing.Commands.CommandScenario<TCommand> scenario)
                {
                    public CommandScenarioChronicleGivenBuilder<TCommand> Given => new();

                    public IEventSequence EventSequence => null!;

                    public Cratis.Chronicle.Testing.EventSequences.EventScenario EventScenario => new();
                }
            }

            public static class CommandResultExtensions
            {
                public static void ShouldBeSuccessful(this Cratis.Arc.Testing.Commands.CommandResult result)
                {
                }
            }

            public class CommandScenarioChronicleGivenBuilder<TCommand>
            {
                public CommandScenarioSourceGivenBuilder<TCommand> ForEventSource(EventSourceId eventSourceId) => new();
            }

            public class CommandScenarioSourceGivenBuilder<TCommand>
            {
                public void Events(params object[] events)
                {
                }

                public void ReadModel<TReadModel>(TReadModel readModel)
                    where TReadModel : class
                {
                }
            }
        }

        namespace Cratis.Chronicle.Testing.ReadModels
        {
            public class ReadModelScenario<TReadModel>
                where TReadModel : class
            {
                public TReadModel? Instance => null;

                public ReadModelScenarioGivenBuilder<TReadModel> Given => new();
            }

            public class ReadModelScenarioGivenBuilder<TReadModel>
                where TReadModel : class
            {
                public ReadModelSourceGivenBuilder<TReadModel> ForEventSource(EventSourceId eventSourceId) => new();

                public ReadModelSourceGivenBuilder<TReadModel> ForEventSourceId(EventSourceId eventSourceId) => new();
            }

            public class ReadModelSourceGivenBuilder<TReadModel>
                where TReadModel : class
            {
                public Task Events(params object[] events) => Task.CompletedTask;

                public Task ReadModel(TReadModel readModel) => Task.CompletedTask;
            }
        }

        namespace Cratis.Chronicle.Testing.Reactors
        {
            public class ReactorScenario<TReactor>
            {
                public ReactorScenarioGivenBuilder<TReactor> Given => new();
            }

            public class ReactorScenarioGivenBuilder<TReactor>
            {
                public ReactorSourceGivenBuilder<TReactor> ForEventSource(EventSourceId eventSourceId) => new();
            }

            public class ReactorSourceGivenBuilder<TReactor>
            {
                public Task Events(params object[] events) => Task.CompletedTask;
            }
        }

        namespace Cratis.Chronicle.XUnit.Integration
        {
            public static class HttpClientExtensions
            {
                public static Task<Cratis.Arc.Testing.Commands.Result> ExecuteCommand<TCommand>(
                    this HttpClient client,
                    string requestUri,
                    TCommand command) => Task.FromResult(new Cratis.Arc.Testing.Commands.Result());
            }
        }

        namespace Cratis.Chronicle.Testing.EventSequences
        {
            public class EventScenario
            {
                public EventScenarioGivenBuilder Given => new();

                public EventScenarioWhenBuilder When => new();

                public IEventSequence EventSequence => null!;
            }

            public class EventScenarioGivenBuilder
            {
                public EventSourceGivenBuilder ForEventSource(EventSourceId eventSourceId) => new();
            }

            public class EventSourceGivenBuilder
            {
                public Task Events(params object[] events) => Task.CompletedTask;
            }

            public class EventScenarioWhenBuilder
            {
                public EventSourceWhenBuilder ForEventSource(EventSourceId eventSourceId) => new();
            }

            public class EventSourceWhenBuilder
            {
                public Task<AppendResult> Events(object @event, params object[] additionalEvents) => Task.FromResult(default(AppendResult)!);
            }

            public static class EventSequenceShouldExtensions
            {
                public static void ShouldHaveAppendedEvent<TEvent>(this IEventSequence sequence, EventSourceId eventSourceId)
                {
                }

                public static Task ShouldHaveAppendedEvent<TEvent>(this IEventSequence sequence, EventSourceId eventSourceId, System.Func<TEvent, bool> predicate) => Task.CompletedTask;

                public static void ShouldHaveTailSequenceNumber(this IEventSequence sequence, int expected)
                {
                }

                public static void ShouldBeSuccessful(this Cratis.Arc.Testing.Commands.Result result)
                {
                }

                public static void ShouldNotBeSuccessful(this Cratis.Arc.Testing.Commands.Result result)
                {
                }

                public static void ShouldHaveConstraintViolationFor(this Cratis.Arc.Testing.Commands.Result result, string constraintName)
                {
                }

                public static void ShouldBeFalse(this bool value)
                {
                }
            }
        }
        """;
}
