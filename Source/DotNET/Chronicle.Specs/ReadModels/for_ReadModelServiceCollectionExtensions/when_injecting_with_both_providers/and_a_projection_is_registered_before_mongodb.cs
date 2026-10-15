// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.when_injecting_with_both_providers;

public class and_a_projection_is_registered_before_mongodb : given.a_read_model_with_both_providers<given.ProjectionOwnedReadModel>
{
    void Establish() => Configure(new(_eventSourceId.Value, "stored"), new(_eventSourceId.Value, "released"), mongoDBFirst: false);

    async Task Because() => _result = await _argumentResolver.Resolve(_handler, _commandContext, _scope.ServiceProvider, allowedSeverity: null);

    [Fact] void should_discover_the_model_in_the_real_mongodb_resolver() => _mongoDBResolver.ReadModelTypes.ShouldContain(typeof(given.ProjectionOwnedReadModel));
    [Fact] void should_discover_the_model_in_the_real_chronicle_resolver() => _chronicleResolver.ReadModelTypes.ShouldContain(typeof(given.ProjectionOwnedReadModel));
    [Fact] void should_load_the_model_from_chronicle() => ShouldHaveLoadedFromChronicle();
    [Fact] void should_release_the_stored_model() => _readModels.Received(1).Release(_stored);
    [Fact] void should_inject_the_released_instance() => _result.Arguments.Single().ShouldBeSame(_released);
    [Fact] void should_not_resolve_a_mongodb_collection() => _mongoCollectionResolutions.ShouldEqual(0);
    [Fact] void should_not_read_from_mongodb() => _mongoCollection.ReceivedCalls().ShouldBeEmpty();
}
