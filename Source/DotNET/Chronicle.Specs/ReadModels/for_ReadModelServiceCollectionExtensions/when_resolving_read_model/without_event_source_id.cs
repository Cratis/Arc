// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.when_resolving_read_model;

public class without_event_source_id : given.a_read_model_resolution
{
    void Establish() => GivenEventSourceIdIsUnspecified();

    void Because() => CatchResolveReadModelException();

    [Fact] void should_throw_unable_to_resolve_read_model_from_command_context() => _exception.ShouldBeOfExactType<UnableToResolveReadModelFromCommandContext>();
    [Fact] void should_not_release_the_read_model() => ShouldNotHaveReleasedReadModel();
}
