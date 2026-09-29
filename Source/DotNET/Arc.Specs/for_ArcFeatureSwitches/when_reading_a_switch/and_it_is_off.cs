// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.for_ArcFeatureSwitches.when_reading_a_switch;

public class and_it_is_off : Specification
{
    string _switchName;
    bool _result;

    void Establish()
    {
        _switchName = $"Cratis.Arc.Specs.{Guid.NewGuid()}";
        AppContext.SetSwitch(_switchName, false);
    }

    void Because() => _result = ArcFeatureSwitches.IsOn(_switchName);

    [Fact] void should_be_off() => _result.ShouldBeFalse();
}
