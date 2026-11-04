// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FrameworkOption;

public class when_the_framework_value_is_missing : Specification
{
    Exception? _exception;

    void Because() => _exception = Catch.Exception(() => FrameworkOption.Parse(["--framework"]));

    [Fact] void should_reject_the_option() => _exception.ShouldBeOfExactType<ArgumentException>();
}
