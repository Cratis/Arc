// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace ValidatorRuntimeApplication;

public class CreateApplicationValidator : CommandValidator<CreateApplication>
{
    public CreateApplicationValidator()
    {
        if (File.Exists(Path.Combine(Path.GetDirectoryName(typeof(CreateApplicationValidator).Assembly.Location)!, "throw-activation")))
        {
            throw new InvalidOperationException("Fixture validator activation failed");
        }

        RuleFor(command => command.Name).NotEmpty().Must(_ => true);
    }

    public new IValidatorDescriptor CreateDescriptor()
    {
        if (File.Exists(Path.Combine(Path.GetDirectoryName(typeof(CreateApplicationValidator).Assembly.Location)!, "throw-descriptor")))
        {
            throw new InvalidOperationException("Fixture validator descriptor failed");
        }

        return base.CreateDescriptor();
    }
}
