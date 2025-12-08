// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;

namespace TestConsole;

[ReadModel]
public record GetGreetingQuery(string Name)
{
    public static GetGreetingQuery GetGreeting(string name)
    {
        return new (name);
    }
}
