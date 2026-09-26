// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

[ReadModel]
[Authorize(Policy = "GuestPermission")]
public record GuestObservableReadModel(string Value)
{
    static int _admitted;
    public static int Admitted => Volatile.Read(ref _admitted);
    public static ISubject<GuestObservableReadModel> All()
    {
        Interlocked.Increment(ref _admitted);
        return new ReplaySubject<GuestObservableReadModel>();
    }
}
