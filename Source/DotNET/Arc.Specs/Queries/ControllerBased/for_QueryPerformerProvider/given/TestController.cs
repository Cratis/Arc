// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.Queries.ControllerBased.for_QueryPerformerProvider.given;

[Route("/api/test")]
public class TestController : ControllerBase
{
    readonly ITestEventStores _eventStores;

    internal TestController(ITestEventStores eventStores)
    {
        _eventStores = eventStores;
    }

    [HttpGet("observe")]
    public ISubject<IEnumerable<string>> AllEventStores(string tenant) => _eventStores.ObserveFor(tenant);
}