// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FrameworkOption;

public class when_parsing_the_default : Specification
{
    ProxyFramework _result;

    void Because() => _result = FrameworkOption.Parse([]);

    [Fact] void should_select_react() => _result.ShouldEqual(ProxyFramework.React);
}
