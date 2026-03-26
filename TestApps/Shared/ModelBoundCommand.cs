// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace TestApps;

[Command]
public record ModelBoundCommand(string StuffToDo)
{
    public string Handle()
    {
        Console.WriteLine("Received command: " + this);
        return $"Doing : {StuffToDo}!";
    }
}
