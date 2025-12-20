// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

public class TestCommand
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class TestCommandValidator : BaseValidator<TestCommand>
{
    public TestCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Email).EmailAddress();
        RuleFor(x => x.Age).GreaterThanOrEqualTo(18);
        RuleFor(x => x.Description).MinimumLength(10).MaximumLength(100);
    }
}

public class TestCommandWithCustomMessages
{
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class TestCommandWithCustomMessagesValidator : BaseValidator<TestCommandWithCustomMessages>
{
    public TestCommandWithCustomMessagesValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required");
        RuleFor(x => x.Age).GreaterThanOrEqualTo(18).WithMessage("Must be at least 18 years old");
    }
}

public class TestCommandWithoutValidator
{
    public string Name { get; set; } = string.Empty;
}
