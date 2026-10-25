// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_getting_parameters;

public class with_a_dictionary_parameter : given.a_model_bound_query_performer
{
    QueryParameters _result;

    void Establish()
    {
        _serviceProviderIsService.IsService(typeof(IDictionary<string, string>)).Returns(true);
        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query));
        _result = _performer.Parameters;
    }

    [Fact] void should_include_the_argument() => _result.Any(parameter => parameter.Name == "parts" && parameter.Type == typeof(IDictionary<string, string>)).ShouldBeTrue();
    [Fact] void should_not_resolve_it_as_a_service() => _performer.Dependencies.ShouldNotContain(typeof(IDictionary<string, string>));

    public record TestReadModel
    {
        public static TestReadModel Query(IDictionary<string, string> parts) => new();
    }
}
