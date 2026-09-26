// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.Testing.for_QueryScenario;

[ReadModel]
public record ScenarioQueryCandidates(string Name)
{
    public static ScenarioQueryCandidates PublicQuery()
    {
        static ScenarioQueryCandidates Local() => new("Public");
        return Local();
    }

    internal static ScenarioQueryCandidates InternalQuery() => new("Internal");
    private static ScenarioQueryCandidates PrivateHelper() => new("Private");
    private static ISubject<ScenarioQueryCandidates> PrivateStream() => new Subject<ScenarioQueryCandidates>();
}
