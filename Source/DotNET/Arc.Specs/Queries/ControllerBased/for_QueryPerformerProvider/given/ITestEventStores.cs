// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.ControllerBased.for_QueryPerformerProvider.given;

public interface ITestEventStores
{
    ISubject<IEnumerable<string>> ObserveFor(string tenant);
}