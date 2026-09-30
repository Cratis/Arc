// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.for_ArcFeatureSwitches.when_reading_a_switch;

public class and_it_is_on : Specification
{
    string _switchName;
    bool _result;

    void Establish()
    {
        _switchName = $"Cratis.Arc.Specs.{Guid.NewGuid()}";
        AppContext.SetSwitch(_switchName, true);
    }

    void Because() => _result = ArcFeatureSwitches.IsOn(_switchName);

    [Fact] void should_be_on() => _result.ShouldBeTrue();
}
