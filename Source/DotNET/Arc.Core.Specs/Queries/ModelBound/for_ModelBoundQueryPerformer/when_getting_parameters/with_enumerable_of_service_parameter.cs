// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_getting_parameters;

public class with_enumerable_of_service_parameter : given.a_model_bound_query_performer
{
    public interface ISomeService;

    public class SomeService : ISomeService;

    public record TestReadModel
    {
        public static IEnumerable<ISomeService>? ReceivedServices { get; set; }

        public static TestReadModel Query(IEnumerable<ISomeService> services)
        {
            ReceivedServices = services;
            return new TestReadModel();
        }
    }

    static readonly IEnumerable<ISomeService> _services = [new SomeService()];

    void Establish()
    {
        // A collection-typed service must keep resolving from the container - the fix narrows classification to
        // element types that are primitives, concepts, or enums, and ISomeService is none of those.
        _serviceProviderIsService.IsService(typeof(IEnumerable<ISomeService>)).Returns(true);

        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query), dependencies: [_services]);
    }

    async Task Because() => await PerformQuery();

    [Fact] void should_have_no_query_parameters() => _performer.Parameters.Count.ShouldEqual(0);
    [Fact] void should_still_be_classified_as_a_dependency() => _performer.Dependencies.Any(t => t == typeof(IEnumerable<ISomeService>)).ShouldBeTrue();
    [Fact] void should_have_injected_the_service_collection() => TestReadModel.ReceivedServices.ShouldEqual(_services);
}
