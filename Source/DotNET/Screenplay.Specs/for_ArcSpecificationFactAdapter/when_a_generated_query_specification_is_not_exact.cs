// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Generation;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

public class when_a_generated_query_specification_is_not_exact : Specification
{
    const string Query = """
            public static async Task<ProjectOverview?> ProjectById(IReadModels readModels, ProjectId projectId) =>
                await readModels.GetInstanceById<ProjectOverview>((EventSourceId)projectId);
        """;

    const string Key = "new ProjectId(Guid.Parse(\"f5a0c7ef-2e5f-4c4d-8d25-62b477b75f4e\"))";
    const string EstablishCall = "_readModels.GetInstanceById<ProjectOverview>((EventSourceId)new ProjectId(Guid.Parse(\"f5a0c7ef-2e5f-4c4d-8d25-62b477b75f4e\"))).Returns(_expected)";
    const string Establish = $"void Establish() => {EstablishCall};";
    const string QueryCall = "ProjectOverview.ProjectById(_readModels, new ProjectId(Guid.Parse(\"f5a0c7ef-2e5f-4c4d-8d25-62b477b75f4e\")))";

    Dictionary<string, InvalidRecovery> _recoveries = null!;

    void Because() => _recoveries = Cases().ToDictionary(_ => _.Name, Analyze, StringComparer.Ordinal);

    [Fact] void should_compile_every_negative_source() => string.Join('\n', _recoveries.Values.SelectMany(_ => _.CompilationErrors)).ShouldEqual(string.Empty);
    [Fact] void should_report_every_negative_once_as_arcsp0001() => string.Join('\n', _recoveries.Where(_ => !_.Value.Contribution.Diagnostics.Select(diagnostic => diagnostic.Code).SequenceEqual(["ARCSP0001"])).Select(_ => $"{_.Key}: {string.Join(',', _.Value.Contribution.Diagnostics.Select(diagnostic => diagnostic.Code))}")).ShouldEqual(string.Empty);
    [Fact] void should_emit_no_partial_recovery_fact_for_any_negative() => string.Join('\n', _recoveries.Where(_ => _.Value.Contribution.Facts.Count != 0).Select(_ => $"{_.Key}: {_.Value.Contribution.Facts.Count}")).ShouldEqual(string.Empty);
    [Fact] void should_cover_every_required_negative_shape() => _recoveries.Keys.ShouldContainOnly([
        "computed argument", "user parse argument", "conditional call", "repeated call", "multiple calls", "unassigned call",
        "incomplete expected", "computed expected", "duplicate assertion", "conditional assertion", "lookalike assertion",
        "property assertion", "reversed assertion", "unrelated same-name query", "spec-only query", "required return",
        "observable return", "collection return", "transport return", "unsupported return", "default input", "extra input",
        "non-specification base", "null read models", "uninitialized read models", "nonreadonly read models",
        "indirect substitute", "nonmatching establish", "conflicting establish", "object expected", "nullable result property",
        "collection result property", "normalized result collision", "key name mismatch", "key type mismatch",
        "null literal", "invalid enum member", "custom concept constructor", "custom read-model constructor",
        "extra await in because", "extra helper in because", "extra establish method", "extra establish statement",
        "block-bodied assertion", "indirect base", "static fields", "required nullable input", "collection input"]);

    static IEnumerable<InvalidCase> Cases()
    {
        const string scenario = GeneratedQuerySpecificationSources.Scenario;
        const string lookalikeAssertion = """
            namespace Projects.Projects.Overview.ListProjects.when_project_by_id_is_queried;

            public static class LookalikeAssertions
            {
                public static void ShouldEqual<T>(T actual, T expected)
                {
                }
            }
            """;
        const string userParse = """
            using System;

            namespace Projects.Projects.Overview.ListProjects.when_project_by_id_is_queried;

            public static class UserGuid
            {
                public static Guid Parse(string value) => Guid.Parse(value);
            }
            """;
        const string unrelatedQuery = """
            using System.Threading.Tasks;
            using Cratis.Chronicle.ReadModels;
            using Projects.Projects.Overview.ListProjects;

            namespace Projects.Projects.Overview.ListProjects.when_project_by_id_is_queried;

            public static class QueryLookalike
            {
                public static Task<ProjectOverview?> ProjectById(IReadModels readModels, ProjectId projectId) =>
                    readModels.GetInstanceById<ProjectOverview>((Cratis.Chronicle.Events.EventSourceId)projectId);
            }
            """;
        const string specificationOnlyQuery = """
            using System;
            using System.Threading.Tasks;
            using Cratis.Arc.Queries.ModelBound;
            using Cratis.Chronicle.ReadModels;
            using Projects.Projects.Overview.ListProjects;

            namespace Projects.Projects.Overview.ListProjects.when_project_by_id_is_queried;

            [ReadModel]
            public record SpecificationOverview(ProjectId Id, ProjectName Name, int Number, DateOnly StartedOn, DateTimeOffset UpdatedAt)
            {
                public static Task<SpecificationOverview?> ProjectById(IReadModels readModels, ProjectId projectId) =>
                    readModels.GetInstanceById<SpecificationOverview>((Cratis.Chronicle.Events.EventSourceId)projectId);
            }
            """;
        const string requiredQuery = """
                public static async Task<ProjectOverview> ProjectById(IReadModels readModels, ProjectId projectId) =>
                    await readModels.GetInstanceById<ProjectOverview>((EventSourceId)projectId);
            """;
        const string observableQuery = """
                public static Task<System.Reactive.Subjects.ISubject<ProjectOverview>> ProjectById(IReadModels readModels, ProjectId projectId) =>
                    Task.FromResult<System.Reactive.Subjects.ISubject<ProjectOverview>>(null!);
            """;
        const string collectionQuery = """
                public static Task<System.Collections.Generic.IReadOnlyList<ProjectOverview>> ProjectById(IReadModels readModels, ProjectId projectId) =>
                    Task.FromResult<System.Collections.Generic.IReadOnlyList<ProjectOverview>>([]);
            """;
        const string transportQuery = """
                public static Task<Microsoft.AspNetCore.Mvc.ActionResult<ProjectOverview>> ProjectById(IReadModels readModels, ProjectId projectId) =>
                    Task.FromResult<Microsoft.AspNetCore.Mvc.ActionResult<ProjectOverview>>(null!);
            """;
        const string unsupportedQuery = """
                public static Task<object> ProjectById(IReadModels readModels, ProjectId projectId) => Task.FromResult<object>(new());
            """;
        const string defaultInputQuery = """
                public static async Task<ProjectOverview?> ProjectById(IReadModels readModels, ProjectId? projectId = null) =>
                    await readModels.GetInstanceById<ProjectOverview>((EventSourceId)projectId!);
            """;
        const string extraInputQuery = """
                public static async Task<ProjectOverview?> ProjectById(IReadModels readModels, ProjectId projectId, int version) =>
                    await readModels.GetInstanceById<ProjectOverview>((EventSourceId)projectId);
            """;
        const string nullableInputQuery = """
                public static async Task<ProjectOverview?> ProjectById(IReadModels readModels, ProjectId? projectId) =>
                    await readModels.GetInstanceById<ProjectOverview>((EventSourceId)projectId!);
            """;
        const string collectionInputQuery = """
                public static async Task<ProjectOverview?> ProjectById(IReadModels readModels, ProjectId[] projectIds) =>
                    await readModels.GetInstanceById<ProjectOverview>((EventSourceId)projectIds[0]);
            """;

        var computedArgument = scenario
            .Replace(QueryCall, "ProjectOverview.ProjectById(_readModels, Key())", StringComparison.Ordinal)
            .Replace("\n    [Fact]", $"\n    static ProjectId Key() => {Key};\n\n    [Fact]", StringComparison.Ordinal);
        var userParseArgument = scenario.Replace(
            QueryCall,
            "ProjectOverview.ProjectById(_readModels, new ProjectId(UserGuid.Parse(\"f5a0c7ef-2e5f-4c4d-8d25-62b477b75f4e\")))",
            StringComparison.Ordinal);
        var conditionalCall = scenario.Replace(
            $"async Task Because() => _result = await {QueryCall};",
            $"async Task Because()\n    {{\n        if (_readModels is not null)\n        {{\n            _result = await {QueryCall};\n        }}\n    }}",
            StringComparison.Ordinal);
        var repeatedCall = scenario.Replace(
            $"async Task Because() => _result = await {QueryCall};",
            $"async Task Because()\n    {{\n        while (_result is null)\n        {{\n            _result = await {QueryCall};\n        }}\n    }}",
            StringComparison.Ordinal);
        var multipleCalls = scenario.Replace(
            $"async Task Because() => _result = await {QueryCall};",
            $"async Task Because()\n    {{\n        _result = await {QueryCall};\n        _result = await {QueryCall};\n    }}",
            StringComparison.Ordinal);
        var unassignedCall = scenario.Replace($"async Task Because() => _result = await {QueryCall};", $"async Task Because() => await {QueryCall};", StringComparison.Ordinal);
        var incompleteApplication = GeneratedQuerySpecificationSources.Application.Replace("DateTimeOffset UpdatedAt)", "DateTimeOffset UpdatedAt = default)", StringComparison.Ordinal);
        var incompleteExpected = scenario.Replace(",\n        DateTimeOffset.Parse(\"2026-02-14T10:15:30+00:00\", CultureInfo.InvariantCulture));", ");", StringComparison.Ordinal);
        var computedExpected = scenario
            .Replace("new ProjectName(\"Screenplay\")", "Name()", StringComparison.Ordinal)
            .Replace("ProjectOverview? _result;", "ProjectOverview? _result;\n\n    static ProjectName Name() => new(\"Screenplay\");", StringComparison.Ordinal);
        var duplicateAssertion = scenario.Replace("[Fact] void should_return_the_expected_read_model() => _result.ShouldEqual(_expected);", "[Fact] void should_return_the_expected_read_model() => _result.ShouldEqual(_expected);\n    [Fact] void should_return_the_same_expected_read_model() => _result.ShouldEqual(_expected);", StringComparison.Ordinal);
        var conditionalAssertion = scenario.Replace("[Fact] void should_return_the_expected_read_model() => _result.ShouldEqual(_expected);", "[Fact] void should_return_the_expected_read_model()\n    {\n        if (_result is not null)\n        {\n            _result.ShouldEqual(_expected);\n        }\n    }", StringComparison.Ordinal);
        var lookalikeAssertionScenario = scenario.Replace("_result.ShouldEqual(_expected)", "LookalikeAssertions.ShouldEqual(_result, _expected)", StringComparison.Ordinal);
        var propertyAssertion = scenario.Replace("_result.ShouldEqual(_expected)", "_result!.Name.ShouldEqual(_expected.Name)", StringComparison.Ordinal);
        var reversedAssertion = scenario.Replace("_result.ShouldEqual(_expected)", "_expected.ShouldEqual(_result)", StringComparison.Ordinal);
        var unrelatedQueryScenario = scenario.Replace("ProjectOverview.ProjectById(_readModels", "QueryLookalike.ProjectById(_readModels", StringComparison.Ordinal);
        var specificationOnlyScenario = scenario.Replace("ProjectOverview", "SpecificationOverview", StringComparison.Ordinal);
        var requiredApplication = GeneratedQuerySpecificationSources.Application.Replace(Query, requiredQuery, StringComparison.Ordinal);
        var observableApplication = GeneratedQuerySpecificationSources.Application.Replace(Query, observableQuery, StringComparison.Ordinal);
        var collectionApplication = GeneratedQuerySpecificationSources.Application.Replace(Query, collectionQuery, StringComparison.Ordinal);
        var transportApplication = GeneratedQuerySpecificationSources.Application.Replace(Query, transportQuery, StringComparison.Ordinal);
        var unsupportedApplication = GeneratedQuerySpecificationSources.Application.Replace(Query, unsupportedQuery, StringComparison.Ordinal);
        var defaultInputApplication = GeneratedQuerySpecificationSources.Application.Replace(Query, defaultInputQuery, StringComparison.Ordinal);
        var extraInputApplication = GeneratedQuerySpecificationSources.Application.Replace(Query, extraInputQuery, StringComparison.Ordinal);
        var extraInputScenario = scenario.Replace(QueryCall, $"ProjectOverview.ProjectById(_readModels, {Key}, 1)", StringComparison.Ordinal);
        var nonSpecificationBase = scenario.Replace(" : Specification", string.Empty, StringComparison.Ordinal);
        var nullReadModels = scenario.Replace("Substitute.For<IReadModels>()", "null!", StringComparison.Ordinal);
        var uninitializedReadModels = scenario.Replace("readonly IReadModels _readModels = Substitute.For<IReadModels>();", "readonly IReadModels _readModels;", StringComparison.Ordinal);
        var nonreadonlyReadModels = scenario.Replace("readonly IReadModels _readModels = Substitute.For<IReadModels>();", "IReadModels _readModels = Substitute.For<IReadModels>();", StringComparison.Ordinal);
        var indirectSubstitute = scenario
            .Replace("Substitute.For<IReadModels>()", "CreateReadModels()", StringComparison.Ordinal)
            .Replace("readonly ProjectOverview _expected", "static IReadModels CreateReadModels() => Substitute.For<IReadModels>();\n\n    readonly ProjectOverview _expected", StringComparison.Ordinal);
        var nonmatchingEstablish = scenario.Replace(Establish, "void Establish() { }", StringComparison.Ordinal);
        var conflictingEstablish = scenario.Replace("f5a0c7ef-2e5f-4c4d-8d25-62b477b75f4e\"))).Returns(_expected)", "4d533cc3-df2c-420f-9fab-5883c666dd23\"))).Returns(_expected)", StringComparison.Ordinal);
        var objectExpected = WithoutEstablish(scenario).Replace(
            "readonly ProjectOverview _expected = new(",
            "readonly object _expected = new ProjectOverview(",
            StringComparison.Ordinal);
        var nullablePropertyApplication = GeneratedQuerySpecificationSources.Application.Replace("ProjectName Name", "ProjectName? Name", StringComparison.Ordinal);
        var collectionPropertyApplication = GeneratedQuerySpecificationSources.Application.Replace("ProjectName Name", "System.Collections.Generic.IReadOnlyList<ProjectName> Names", StringComparison.Ordinal);
        var collectionPropertyScenario = scenario.Replace("new ProjectName(\"Screenplay\"),", "[new ProjectName(\"Screenplay\")],", StringComparison.Ordinal);
        var collidingApplication = GeneratedQuerySpecificationSources.Application.Replace("DateTimeOffset UpdatedAt)", "DateTimeOffset UpdatedAt, string URL, string Url)", StringComparison.Ordinal);
        var collidingScenario = scenario.Replace("DateTimeOffset.Parse(\"2026-02-14T10:15:30+00:00\", CultureInfo.InvariantCulture));", "DateTimeOffset.Parse(\"2026-02-14T10:15:30+00:00\", CultureInfo.InvariantCulture), \"upper\", \"camel\");", StringComparison.Ordinal);
        var keyNameApplication = GeneratedQuerySpecificationSources.Application.Replace("ProjectOverview(ProjectId ProjectId", "ProjectOverview(ProjectId OtherId", StringComparison.Ordinal);
        var keyTypeApplication = GeneratedQuerySpecificationSources.Application.Replace("ProjectOverview(ProjectId ProjectId", "ProjectOverview(Guid ProjectId", StringComparison.Ordinal);
        var keyTypeScenario = scenario.Replace(
            "readonly ProjectOverview _expected = new(\n        new ProjectId(Guid.Parse(\"f5a0c7ef-2e5f-4c4d-8d25-62b477b75f4e\")),",
            "readonly ProjectOverview _expected = new(\n        Guid.Parse(\"f5a0c7ef-2e5f-4c4d-8d25-62b477b75f4e\"),",
            StringComparison.Ordinal);
        var nullLiteral = WithoutEstablish(scenario).Replace(QueryCall, "ProjectOverview.ProjectById(_readModels, null!)", StringComparison.Ordinal);
        var enumApplication = GeneratedQuerySpecificationSources.Application
            .Replace("[ReadModel]", "public enum ProjectStatus { Active }\n\n[ReadModel]", StringComparison.Ordinal)
            .Replace("DateTimeOffset UpdatedAt)", "DateTimeOffset UpdatedAt, ProjectStatus Status)", StringComparison.Ordinal);
        var invalidEnumMember = scenario.Replace(
            "DateTimeOffset.Parse(\"2026-02-14T10:15:30+00:00\", CultureInfo.InvariantCulture));",
            "DateTimeOffset.Parse(\"2026-02-14T10:15:30+00:00\", CultureInfo.InvariantCulture), (ProjectStatus)99);",
            StringComparison.Ordinal);
        var customConceptApplication = GeneratedQuerySpecificationSources.Application.Replace(
            "public record ProjectId(Guid Value) : EventSourceId<Guid>(Value);",
            "public record ProjectId(Guid Value) : EventSourceId<Guid>(Value)\n{\n    public ProjectId(string value) : this(Guid.Parse(value)) { }\n}",
            StringComparison.Ordinal);
        var customConceptScenario = scenario.Replace(
            "new ProjectId(Guid.Parse(\"f5a0c7ef-2e5f-4c4d-8d25-62b477b75f4e\"))",
            "new ProjectId(\"f5a0c7ef-2e5f-4c4d-8d25-62b477b75f4e\")",
            StringComparison.Ordinal);
        const string customReadModel = """
            public record ProjectOverview
            {
                public ProjectId ProjectId { get; }
                public ProjectName Name { get; }
                public int Number { get; }
                public DateOnly StartedOn { get; }
                public DateTimeOffset UpdatedAt { get; }

                public ProjectOverview(ProjectId ProjectId, ProjectName Name, int Number, DateOnly StartedOn, DateTimeOffset UpdatedAt)
                {
                    this.ProjectId = ProjectId;
                    this.Name = Name;
                    this.Number = Number;
                    this.StartedOn = StartedOn;
                    this.UpdatedAt = UpdatedAt;
                }
            """;
        var customReadModelApplication = GeneratedQuerySpecificationSources.Application.Replace(
            "public record ProjectOverview(ProjectId ProjectId, ProjectName Name, int Number, DateOnly StartedOn, DateTimeOffset UpdatedAt)\n{",
            customReadModel,
            StringComparison.Ordinal);
        var extraAwait = scenario.Replace(
            $"async Task Because() => _result = await {QueryCall};",
            $"async Task Because()\n    {{\n        await Task.Yield();\n        _result = await {QueryCall};\n    }}",
            StringComparison.Ordinal);
        var extraHelper = scenario
            .Replace(
                $"async Task Because() => _result = await {QueryCall};",
                $"async Task Because()\n    {{\n        Observe();\n        _result = await {QueryCall};\n    }}",
                StringComparison.Ordinal)
            .Replace("\n    [Fact]", "\n    static void Observe() { }\n\n    [Fact]", StringComparison.Ordinal);
        var extraEstablishMethod = scenario.Replace(Establish, $"{Establish}\n\n    void Establish(int ignored) {{ }}", StringComparison.Ordinal);
        var extraEstablishStatement = scenario.Replace(
            Establish,
            $"void Establish()\n    {{\n        {EstablishCall};\n        _ = _expected;\n    }}",
            StringComparison.Ordinal);
        var blockBodiedAssertion = scenario.Replace(
            "[Fact] void should_return_the_expected_read_model() => _result.ShouldEqual(_expected);",
            "[Fact] void should_return_the_expected_read_model()\n    {\n        _result.ShouldEqual(_expected);\n    }",
            StringComparison.Ordinal);
        var indirectBase = scenario.Replace(" : Specification", " : QuerySpecification", StringComparison.Ordinal);
        const string indirectBaseType = """
            using Cratis.Specifications;

            namespace Projects.Projects.Overview.ListProjects.when_project_by_id_is_queried;

            public abstract class QuerySpecification : Specification;
            """;
        var staticFields = scenario
            .Replace("readonly IReadModels _readModels", "static readonly IReadModels _readModels", StringComparison.Ordinal)
            .Replace("readonly ProjectOverview _expected", "static readonly ProjectOverview _expected", StringComparison.Ordinal)
            .Replace("ProjectOverview? _result;", "static ProjectOverview? _result;", StringComparison.Ordinal);
        var nullableInputApplication = GeneratedQuerySpecificationSources.Application.Replace(Query, nullableInputQuery, StringComparison.Ordinal);
        var collectionInputApplication = GeneratedQuerySpecificationSources.Application.Replace(Query, collectionInputQuery, StringComparison.Ordinal);
        var collectionInputScenario = scenario.Replace(QueryCall, $"ProjectOverview.ProjectById(_readModels, [{Key}])", StringComparison.Ordinal);

        return
        [
            new("computed argument", GeneratedQuerySpecificationSources.Application, computedArgument),
            new("user parse argument", GeneratedQuerySpecificationSources.Application, userParseArgument, userParse),
            new("conditional call", GeneratedQuerySpecificationSources.Application, conditionalCall),
            new("repeated call", GeneratedQuerySpecificationSources.Application, repeatedCall),
            new("multiple calls", GeneratedQuerySpecificationSources.Application, multipleCalls),
            new("unassigned call", GeneratedQuerySpecificationSources.Application, unassignedCall),
            new("incomplete expected", incompleteApplication, incompleteExpected),
            new("computed expected", GeneratedQuerySpecificationSources.Application, computedExpected),
            new("duplicate assertion", GeneratedQuerySpecificationSources.Application, duplicateAssertion),
            new("conditional assertion", GeneratedQuerySpecificationSources.Application, conditionalAssertion),
            new("lookalike assertion", GeneratedQuerySpecificationSources.Application, lookalikeAssertionScenario, lookalikeAssertion),
            new("property assertion", GeneratedQuerySpecificationSources.Application, propertyAssertion),
            new("reversed assertion", GeneratedQuerySpecificationSources.Application, reversedAssertion),
            new("unrelated same-name query", GeneratedQuerySpecificationSources.Application, unrelatedQueryScenario, unrelatedQuery),
            new("spec-only query", GeneratedQuerySpecificationSources.Application, specificationOnlyScenario, specificationOnlyQuery),
            new("required return", requiredApplication, scenario),
            new("observable return", observableApplication, ResultOf(scenario, "System.Reactive.Subjects.ISubject<ProjectOverview>")),
            new("collection return", collectionApplication, ResultOf(scenario, "System.Collections.Generic.IReadOnlyList<ProjectOverview>")),
            new("transport return", transportApplication, ResultOf(scenario, "Microsoft.AspNetCore.Mvc.ActionResult<ProjectOverview>")),
            new("unsupported return", unsupportedApplication, ResultOf(scenario, "object")),
            new("default input", defaultInputApplication, scenario),
            new("extra input", extraInputApplication, extraInputScenario),
            new("non-specification base", GeneratedQuerySpecificationSources.Application, nonSpecificationBase),
            new("null read models", GeneratedQuerySpecificationSources.Application, nullReadModels),
            new("uninitialized read models", GeneratedQuerySpecificationSources.Application, uninitializedReadModels),
            new("nonreadonly read models", GeneratedQuerySpecificationSources.Application, nonreadonlyReadModels),
            new("indirect substitute", GeneratedQuerySpecificationSources.Application, indirectSubstitute),
            new("nonmatching establish", GeneratedQuerySpecificationSources.Application, nonmatchingEstablish),
            new("conflicting establish", GeneratedQuerySpecificationSources.Application, conflictingEstablish),
            new("object expected", GeneratedQuerySpecificationSources.Application, objectExpected),
            new("nullable result property", nullablePropertyApplication, scenario),
            new("collection result property", collectionPropertyApplication, collectionPropertyScenario),
            new("normalized result collision", collidingApplication, collidingScenario),
            new("key name mismatch", keyNameApplication, scenario),
            new("key type mismatch", keyTypeApplication, keyTypeScenario),
            new("null literal", GeneratedQuerySpecificationSources.Application, nullLiteral),
            new("invalid enum member", enumApplication, invalidEnumMember),
            new("custom concept constructor", customConceptApplication, customConceptScenario),
            new("custom read-model constructor", customReadModelApplication, scenario),
            new("extra await in because", GeneratedQuerySpecificationSources.Application, extraAwait),
            new("extra helper in because", GeneratedQuerySpecificationSources.Application, extraHelper),
            new("extra establish method", GeneratedQuerySpecificationSources.Application, extraEstablishMethod),
            new("extra establish statement", GeneratedQuerySpecificationSources.Application, extraEstablishStatement),
            new("block-bodied assertion", GeneratedQuerySpecificationSources.Application, blockBodiedAssertion),
            new("indirect base", GeneratedQuerySpecificationSources.Application, indirectBase, indirectBaseType),
            new("static fields", GeneratedQuerySpecificationSources.Application, staticFields),
            new("required nullable input", nullableInputApplication, scenario),
            new("collection input", collectionInputApplication, collectionInputScenario)
        ];
    }

    static string WithoutEstablish(string scenario) => scenario.Replace($"\n    {Establish}\n", string.Empty, StringComparison.Ordinal);

    static string ResultOf(string scenario, string type) => scenario
        .Replace("ProjectOverview? _result;", $"{type}? _result;", StringComparison.Ordinal)
        .Replace("_result.ShouldEqual(_expected)", $"Cratis.Specifications.ShouldEqualityExtensions.ShouldEqual<{type}?>(_result, null)", StringComparison.Ordinal);

    static InvalidRecovery Analyze(InvalidCase invalid)
    {
        var application = Analyzed.Project(
            "Projects",
            [],
            ("Testing/Framework.cs", GeneratedQuerySpecificationSources.Framework),
            ("Projects/Overview/ListProjects/ProjectOverview.cs", invalid.Application));
        var specificationSources = invalid.ExtraSources
            .Select((source, index) => ($"Projects/Overview/ListProjects/Extra{index}.cs", source))
            .Append(("Projects/Overview/ListProjects/when_project_by_id_is_queried.cs", invalid.Scenario))
            .ToArray();
        var specifications = Analyzed.Project("Projects.Specifications", [application.ToMetadataReference()], specificationSources);
        var errors = Analyzed.ErrorsIn(application).Concat(Analyzed.ErrorsIn(specifications)).ToArray();
        var contribution = new ArcSpecificationFactAdapter().Analyze(
            new DotNetAnalysisContext([
                SourceProjects.Create("Projects", DotNetProjectRole.Application, application),
                SourceProjects.Create("Projects.Specifications", DotNetProjectRole.Specifications, specifications)
            ]),
            new DotNetAdapterOptions { Module = "Projects", NamespaceSegmentsToSkip = 1 });
        return new(contribution, errors);
    }

    sealed record InvalidCase(string Name, string Application, string Scenario, params string[] ExtraSources);
    sealed record InvalidRecovery(AdapterContribution Contribution, IReadOnlyList<string> CompilationErrors);
}
