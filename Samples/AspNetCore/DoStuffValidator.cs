// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace AspNetCore;

public class DoStuffValidator : CommandValidator<DoStuff>
{
    public DoStuffValidator()
    {
        RuleFor(x => x.StuffToDo).NotEmpty();
    }
}
