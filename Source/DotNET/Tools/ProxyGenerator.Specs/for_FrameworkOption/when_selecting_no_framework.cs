// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FrameworkOption;

public class when_selecting_no_framework : Specification
{
    ProxyFramework _result;

    void Because() => _result = FrameworkOption.Parse(["--framework=none"]);

    [Fact] void should_select_no_framework() => _result.ShouldEqual(ProxyFramework.None);
}
