// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore;

[Route("api/observable-queries")]
public class ObservableQueries : ControllerBase
{
    [HttpGet("messages")]
    public ISubject<string> Messages()
    {
        var subject = new Subject<string>();

        _ = Task.Run(async () =>
        {
            var count = 0;
            while (true)
            {
                await Task.Delay(1000);
                subject.OnNext($"Message {++count}");
            }
        });

        return subject;
    }
}