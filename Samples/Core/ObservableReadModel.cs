// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;

namespace Core;

[ReadModel]
public record ObservableReadModel(string Message)
{
    public static ISubject<ObservableReadModel> Messages()
    {
        var subject = new Subject<ObservableReadModel>();

        _ = Task.Run(async () =>
        {
            var count = 0;
            while (true)
            {
                await Task.Delay(1000);
                subject.OnNext(new ObservableReadModel($"Message {++count}"));
            }
        });

        return subject;
    }
}