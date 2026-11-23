// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_DeclarationNames;

public class when_collecting_the_completed_model : Specification
{
    HashSet<string> _names;

    void Because() => _names = DeclarationNames.In(Application(), 0).Select(new ScreenplayNaming().ToDeclarationName).ToHashSet(StringComparer.Ordinal);

    [Theory]
    [InlineData("DomainName")]
    [InlineData("ModuleName")]
    [InlineData("FeatureName")]
    [InlineData("SliceName")]
    [InlineData("ConceptName")]
    [InlineData("ShapeName")]
    [InlineData("PolicyName")]
    [InlineData("RoleName")]
    [InlineData("NamedPolicy")]
    [InlineData("Authenticated")]
    [InlineData("ContextName")]
    [InlineData("ProvidedScope")]
    [InlineData("ImportedEvent")]
    [InlineData("EventName")]
    [InlineData("CommandName")]
    [InlineData("QueryName")]
    [InlineData("ProjectionName")]
    [InlineData("ReactorName")]
    [InlineData("ConstraintName")]
    [InlineData("ScreenName")]
    [InlineData("ScenarioName")]
    [InlineData("ReadModelName")]
    [InlineData("AuthoringReadModel")]
    [InlineData("OperationName")]
    [InlineData("SystemName")]
    [InlineData("AbilityName")]
    [InlineData("AbilitySystem")]
    [InlineData("AudienceName")]
    [InlineData("AvailabilityName")]
    [InlineData("RouteSource")]
    [InlineData("RouteStream")]
    [InlineData("SpecSource")]
    [InlineData("SpecStream")]
    [InlineData("ObserverSource")]
    [InlineData("ObserverStream")]
    public void should_reserve_every_declared_kind(string name) => _names.ShouldContain(name);

    [Theory]
    [InlineData("EnTry", "en-try")]
    [InlineData("Café", "Cafe\u0301")]
    public void should_compare_candidate_names_in_the_same_normalized_namespace(string candidate, string occupied)
    {
        var compilation = Analyzed.Compile((Analyzed.SlicePath, $"using Cratis.Concepts; namespace Library; public record {candidate}(string Value) : ConceptAs<string>(Value);"));
        var types = new TypeRegistry();
        types.RegisterDeclarations([compilation.GetTypeByMetadataName($"Library.{candidate}")!], [occupied]);
        types.Concepts.ShouldBeEmpty();
    }

    static ApplicationModel Application() => new(
        "DomainName",
        "ModuleName",
        [new ConceptModel("ConceptName", ScreenplayPrimitive.String, false, [], [])],
        [new PolicyModel("policy-name", new AuthenticatedRequirement())],
        [Slice()],
        [new TypeModel("ShapeName", [])])
    {
        Imports = ["External.ImportedEvent"],
        ContextValues = [new("ContextName", "QueryName", "key", ["ProvidedScope"], true, "source")]
    };

    static SliceModel Slice() => SliceModel.Empty("ModuleName.FeatureName.SourceSlice", "SliceName", SliceKind.StateChange) with
    {
        Events = [new("EventName", [], [])],
        Commands = [Command()],
        Queries = [new("QueryName", new("ReadModelName", true, false), null, [], new(true, []))],
        ReadModels = [new("ReadModelName", [])],
        Projections = [new("Library.Projection-Name", "ReadModelName", "event-log", ProjectionAutoMapMode.Disabled, false, ProjectionScopeModel.Empty)],
        Reactors = [new("ReactorName", ["EventName"], false, null, new("ObserverSource", "ObserverStream", true)) { ObservationDefinitions = [new("Observer", "ObserverSource", ["ObserverStream"])] }],
        Constraints = [new CustomConstraintModel("ConstraintName", null)],
        Screens = [new("ScreenName", "screen.tsx")],
        Specifications = [new("ScenarioName", [new("EventName", SpecificationStateKind.Event, []) { Route = new("SpecSource", "SpecStream", null) }], null, [], [])]
    };

    static CommandModel Command() => new("CommandName", null, [], new(true, ["role-name"]) { Policies = ["named-policy"] }, [], [], null, null)
    {
        Availability = new([new(["AudienceName"], "AvailabilityName")]),
        Authoring = new()
        {
            Reads = [new("AuthoringReadModel", "alias", "key")],
            Route = new("RouteSource", "RouteStream", null, null, null),
            Operations = [new("OperationName", "SystemName", null, [], [], null, false) { ExecuteInvocations = [new("AbilitySystem", "AbilityName", [], [])] }]
        }
    };
}
