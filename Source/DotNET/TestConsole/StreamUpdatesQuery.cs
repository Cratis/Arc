// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;

namespace TestConsole;

[ReadModel]
public record StreamUpdate(string Update)
{
    public static async IAsyncEnumerable<StreamUpdate> StreamUpdates()
    {
        for (var i = 1; i <= 10; i++)
        {
            await Task.Delay(1000);
            yield return new StreamUpdate($"Update #{i} at {DateTime.Now:HH:mm:ss}");
        }
    }
}
