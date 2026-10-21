// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryMethod.given;

public partial record SelectedReadModel
{
    [Query]
    [Path("/api/selection/by-id")]
    [QueryHttpMethod(QueryHttpMethod.Query)]
    [AllowAnonymous]
    public static SelectedReadModel? ById(string id) => new(id);

    [Query]
    public static ISubject<IEnumerable<SelectedReadModel>> Observe() => new BehaviorSubject<IEnumerable<SelectedReadModel>>([new("initial")]);
}
