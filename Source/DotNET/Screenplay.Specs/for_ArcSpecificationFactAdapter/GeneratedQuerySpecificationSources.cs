// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

static class GeneratedQuerySpecificationSources
{
    public const string Framework = """
        namespace Projects.Framework
        {
            public static class Marker;
        }

        namespace Microsoft.AspNetCore.Mvc
        {
            public abstract class ActionResult;
            public sealed class ActionResult<T> : ActionResult;
        }
        """;

    public const string Application = """
        using System;
        using System.Threading.Tasks;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.ReadModels;
        using Cratis.Concepts;

        namespace Projects.Projects.Overview.ListProjects;

        public record ProjectId(Guid Value) : EventSourceId<Guid>(Value);

        public record ProjectName(string Value) : ConceptAs<string>(Value);

        [ReadModel]
        public record ProjectOverview(ProjectId ProjectId, ProjectName Name, int Number, DateOnly StartedOn, DateTimeOffset UpdatedAt)
        {
            public static async Task<ProjectOverview?> ProjectById(IReadModels readModels, ProjectId projectId) =>
                await readModels.GetInstanceById<ProjectOverview>((EventSourceId)projectId);
        }
        """;

    public const string Scenario = """
        using System;
        using System.Globalization;
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.ReadModels;
        using Cratis.Specifications;
        using NSubstitute;
        using Projects.Projects.Overview.ListProjects;
        using Xunit;

        namespace Projects.Projects.Overview.ListProjects.when_project_by_id_is_queried;

        public class when_project_by_id_is_queried : Specification
        {
            readonly IReadModels _readModels = Substitute.For<IReadModels>();
            readonly ProjectOverview _expected = new(
                new ProjectId(Guid.Parse("f5a0c7ef-2e5f-4c4d-8d25-62b477b75f4e")),
                new ProjectName("Screenplay"),
                42,
                DateOnly.Parse("2026-02-14", CultureInfo.InvariantCulture),
                DateTimeOffset.Parse("2026-02-14T10:15:30+00:00", CultureInfo.InvariantCulture));
            ProjectOverview? _result;

            void Establish() => _readModels.GetInstanceById<ProjectOverview>((EventSourceId)new ProjectId(Guid.Parse("f5a0c7ef-2e5f-4c4d-8d25-62b477b75f4e"))).Returns(_expected);

            async Task Because() => _result = await ProjectOverview.ProjectById(_readModels, new ProjectId(Guid.Parse("f5a0c7ef-2e5f-4c4d-8d25-62b477b75f4e")));

            [Fact] void should_return_the_expected_read_model() => _result.ShouldEqual(_expected);
        }
        """;

    public const string PartialScenarioFields = """
        using System;
        using System.Globalization;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.ReadModels;
        using Cratis.Specifications;
        using NSubstitute;
        using Projects.Projects.Overview.ListProjects;

        namespace Projects.Projects.Overview.ListProjects.when_project_by_id_is_queried;

        public partial class when_project_by_id_is_queried : Specification
        {
            readonly IReadModels _readModels = Substitute.For<IReadModels>();
            readonly ProjectOverview _expected = new(
                new ProjectId(Guid.Parse("f5a0c7ef-2e5f-4c4d-8d25-62b477b75f4e")),
                new ProjectName("Screenplay"),
                42,
                DateOnly.Parse("2026-02-14", CultureInfo.InvariantCulture),
                DateTimeOffset.Parse("2026-02-14T10:15:30+00:00", CultureInfo.InvariantCulture));
            ProjectOverview? _result;

            void Establish() => _readModels.GetInstanceById<ProjectOverview>((EventSourceId)new ProjectId(Guid.Parse("f5a0c7ef-2e5f-4c4d-8d25-62b477b75f4e"))).Returns(_expected);
        }
        """;

    public const string PartialScenarioBehavior = """
        using System;
        using System.Threading.Tasks;
        using Cratis.Specifications;
        using Projects.Projects.Overview.ListProjects;
        using Xunit;

        namespace Projects.Projects.Overview.ListProjects.when_project_by_id_is_queried;

        public partial class when_project_by_id_is_queried
        {
            async Task Because() => _result = await ProjectOverview.ProjectById(_readModels, new ProjectId(Guid.Parse("f5a0c7ef-2e5f-4c4d-8d25-62b477b75f4e")));

            [Fact] void should_return_the_expected_read_model() => _result.ShouldEqual(_expected);
        }
        """;
}
