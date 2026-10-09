// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventTagsValuesProvider.when_providing;

public class without_tags : given.an_event_tags_provider
{
    void Because() => _values = _provider.Provide(new object());

    [Fact] void should_not_write_the_key() => _values.ShouldBeEmpty();
}
