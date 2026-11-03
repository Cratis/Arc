// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

public class from_a_stage_shaped_command_keyed_by_its_identifier : a_protected_document
{
    /// <summary>Builds V25's identifier-keyed update with or without protected read admission.</summary>
    /// <param name="protect">Whether read protection is explicitly on.</param>
    /// <param name="optional">Whether the event's value is optional.</param>
    /// <returns>The complete source compilation unit.</returns>
    public static string Source(bool protect, bool optional = false)
    {
        // Command source: Stage's .ai-work/3558/EsmGoldenVectors_V25.Write.cs.txt.
        var command = $$"""
        [Command]
        [BlockOnValidationSeverity(ValidationResultSeverity.Error)]
        [AllowAnonymous]
        public record Update(string Id, string Value) : ICanProvideEventSourceId
        {
            /// <inheritdoc/>
            public EventSourceId GetEventSourceId() => Id;

            public OneOf<EventForEventSourceId, ValidationResult> Handle([LookUpBy(nameof(Update.Id))] View? view)
            {
                if (view is null) return OneOf<EventForEventSourceId, ValidationResult>.FromT1(ValidationResult.Error("View '" + Id + "' does not exist."));
                if (!(!object.Equals(view.Value, ""))) return OneOf<EventForEventSourceId, ValidationResult>.FromT1(ValidationResult.Error("Command requirement was not met."));
                return OneOf<EventForEventSourceId, ValidationResult>.FromT0(new EventForEventSourceId(Id, new Recorded(view.Value)));
            }
        }

        /// <summary>
        /// The event that occurs when removed.
        /// </summary>
        [EventType]
        public record Removed();

        /// <summary>
        /// The event that occurs when recorded.
        /// </summary>
        [EventType]
        public record Recorded(string{{(optional ? "?" : "")}} Value);
        """;

        // Model source: Stage's .ai-work/3558/EsmGoldenVectors_V25.recovered.play.
        // Build projects Recorded's event-source id and value, and removes with Removed. Model-bound naming uses View.
        var view = $$"""
        [ReadModel]
        [NoAutoMap]
        [FromEvent<Recorded>]
        [RemovedWith<Removed>]
        public record View(
            [property: SetFromContext<Recorded>(nameof(EventContext.EventSourceId))] string Id,
            [property: SetFrom<Recorded>(nameof(Recorded.Value))] string{{(optional ? "?" : "")}} Value)
        {
            public static View? Get(IReadModels readModels, EventSourceId id) => null;
        }
        """;
        var attributes = "using OneOf;\nusing Cratis.Arc.Authorization;\nusing Cratis.Chronicle.EventSequences;\nusing Cratis.Chronicle.Projections.ModelBound;\n" +
            (protect ? "[assembly: ReadProtection(ReadProtectionMode.On)]" : string.Empty);
        return Library(command, view, attributes);
    }

    void Because() => Generate(Source(true));

    [Fact] void should_read_by_the_command_identifier() => Result.Source.ShouldContain("reads View by id");
    [Fact] void should_preserve_the_read_backed_requirement() => Result.Source.ShouldContain("require View.value != \"\"");
    [Fact] void should_map_the_read_value() => Result.Source.ShouldContain("value = View.value");

    [Fact]
    void should_bind_at_v25()
    {
        var compilation = Analyzed.Compile((Analyzed.SlicePath, Source(true)));
        var errors = compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).ToArray();
        var diagnostics = string.Join(Environment.NewLine, Result.Diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Severity}: {diagnostic.Message}"));
        var binding = string.Join(Environment.NewLine, Bound.Diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Severity}: {diagnostic.Message}"));
        TestContext.Current.TestOutputHelper!.WriteLine($"C# errors: {string.Join(Environment.NewLine, errors.Select(error => error.ToString()))}\nDOCUMENT:\n{Result.Source}\nGENERATOR DIAGNOSTICS:\n{diagnostics}\nBINDING DIAGNOSTICS:\n{binding}\nAUTHORING DOCUMENT:\n{Authoring.Source}");
        errors.ShouldBeEmpty();
        Result.Source.ShouldContain("command reads protected");
        Result.Source.ShouldContain("reads View by id");
        Result.Source.ShouldContain("require View.value != \"\"");
        Result.Source.ShouldContain("value = View.value");
        AssertBindsAtV25(Result);
    }

    [Fact] void should_round_trip() => RoundTrip.IsStable.ShouldBeTrue();
    [Fact] void should_recover_the_same_read_in_authoring_mode() => Authoring.Source.ShouldContain("reads View by id");
}
