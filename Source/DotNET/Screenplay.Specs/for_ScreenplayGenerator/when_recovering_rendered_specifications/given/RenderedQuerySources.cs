// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_recovering_rendered_specifications.given;

/// <summary>
/// A model-bound query and the scenario Stage renders for it, compiled as one project.
/// </summary>
public static class RenderedQuerySources
{
    public const string Scenario = GeneratedQuerySpecificationSources.Scenario;
    public const string QueryCall = "ProjectOverview.ProjectById(_readModels, new ProjectId(Guid.Parse(\"f5a0c7ef-2e5f-4c4d-8d25-62b477b75f4e\")))";
    public const string QueryStep = $"async Task Because() => _result = await {QueryCall};";
    public const string Assertion = "_result.ShouldEqual(_expected)";

    /// <summary>
    /// A static class with the same query method as the application, which the application does not declare as a query.
    /// </summary>
    public const string QueryLookalike = """
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

    public static (string Path, string Text)[] Sources(string scenario, string? companion = null) =>
    [
        ("Library/Projects/Overview/ListProjects/ProjectOverview.cs", GeneratedQuerySpecificationSources.Application),
        ("Library/Projects/Overview/ListProjects/when_project_by_id_is_queried.cs", scenario),
        .. companion is null ? [] : new[] { ("Library/Projects/Overview/ListProjects/QueryLookalike.cs", companion) }
    ];
}
