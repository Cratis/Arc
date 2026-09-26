// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.Testing.for_QueryScenario;

[ReadModel]
public record ScenarioReadModel(string Name)
{
    [AllowAnonymous]
    public static ScenarioReadModel All() => new("All");

    [AllowAnonymous]
    public static ScenarioReadModel ByName(ScenarioName name) => new(name.Value);

    [Authorize(Policy = "CanRead")]
    public static ScenarioReadModel Restricted(ScenarioName name) => new(name.Value);

    [Authorize(Roles = "Administrator")]
    public static ScenarioReadModel RoleRestricted() => new("Allowed by role");

    [Authorize(Policy = "CanRead")]
    public static ScenarioReadModel Scoped(TrackedQueryScope scope) => new(scope.Disposed ? "Disposed too early" : "In scope");

    [Authorize(Policy = "CanRead", AuthenticationSchemes = "TestScheme")]
    public static ScenarioReadModel SchemeRestricted() => new("Should not run");

    [AllowAnonymous]
    public static ISubject<ScenarioReadModel> Stream() => new Subject<ScenarioReadModel>();
}
