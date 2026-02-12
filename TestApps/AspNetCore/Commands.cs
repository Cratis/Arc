// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace AspNetCore;

[Route("api/commands")]
public class Commands : ControllerBase
{
    [HttpPost("do-stuff")]
    public string DoStuff([FromBody] DoStuff command)
    {
        Console.WriteLine("Received command: " + command);
        return $"Doing : {command.StuffToDo}!";
    }
}