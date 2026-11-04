// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FrameworkOption;

public class when_selecting_an_invalid_framework : Specification
{
    Exception? _exception;

    void Because() => _exception = Catch.Exception(() => FrameworkOption.Parse(["--framework=angular"]));

    [Fact] void should_reject_the_value() => _exception.ShouldBeOfExactType<ArgumentException>();
}
