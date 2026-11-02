// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_LookUpByAttribute;

public class when_declared_with_a_key_provider : Specification
{
    ILookUpByKeyProvider _first;
    ILookUpByKeyProvider _second;

    void Because()
    {
        _first = new LookUpByAttribute<CallerSubject>();
        _second = new LookUpByAttribute<CallerSubject>();
    }

    [Fact] void should_expose_the_provider_type() => _first.ProviderType.ShouldEqual(typeof(CallerSubject));
    [Fact] void should_return_the_same_provider_instance_every_time() => _first.Provider.ShouldBeSame(_second.Provider);
}
