// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Commands.for_CommandOptionsValidator.when_validating;

public class with_a_defined_maximum : Specification
{
    ValidateOptionsResult[] _results;

    void Because() => _results = Enum.GetValues<ValidationResultSeverity>()
        .Select(severity => new CommandOptionsValidator().Validate(null, new ArcOptions { Command = new() { MaximumCallerAllowedSeverity = severity } })).ToArray();

    [Fact] void should_accept_every_defined_maximum() => _results.All(result => result.Succeeded).ShouldBeTrue();
}
