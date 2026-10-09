// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventTagsValuesProvider.when_providing;

public class without_property_or_constant : given.an_event_tags_provider
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => _provider.Provide(new TaggedCommand()));

    [Fact] void should_reject_missing_configuration() => _error.ShouldBeOfExactType<AmbiguousEventTagValue>();

    [EventTag("project")]
    record TaggedCommand;
}
