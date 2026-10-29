// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Commands.for_CommandOptionsValidator.when_validating;

public class with_an_out_of_range_maximum : Specification
{
    ValidateOptionsResult _above;
    ValidateOptionsResult _below;

    void Because()
    {
        var validator = new CommandOptionsValidator();
        _above = validator.Validate(null, new ArcOptions { Command = new() { MaximumCallerAllowedSeverity = (ValidationResultSeverity)4 } });
        _below = validator.Validate(null, new ArcOptions { Command = new() { MaximumCallerAllowedSeverity = (ValidationResultSeverity)(-1) } });
    }

    [Fact] void should_reject_a_maximum_above_error() => _above.Failed.ShouldBeTrue();
    [Fact] void should_reject_a_maximum_below_unknown() => _below.Failed.ShouldBeTrue();
}
