// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_EventTagsValuesProvider.given;

public class an_event_tags_provider : Specification
{
    protected EventTagsValuesProvider _provider;
    protected ICanProvideCommandEventTags _applicationProvider;
    protected CommandContextValues _values;

    void Establish()
    {
        _applicationProvider = Substitute.For<ICanProvideCommandEventTags>();
        _applicationProvider.GetEventTags(Arg.Any<object>()).Returns([]);
        _provider = new(new KnownInstancesOf<ICanProvideCommandEventTags>([_applicationProvider]));
    }
}
