// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandOptionsValidator.when_validating;

public class with_the_default : Specification
{
    Microsoft.Extensions.Options.ValidateOptionsResult _result;

    void Because() => _result = new CommandOptionsValidator().Validate(null, new ArcOptions());

    [Fact] void should_accept_the_default() => _result.Succeeded.ShouldBeTrue();
}
