// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_getting_parameters;

public class with_additional_collection_element_types : given.a_model_bound_query_performer
{
    public record TestReadModel
    {
        public static TestReadModel Query(IEnumerable<DateOnly> dates, IEnumerable<TimeOnly> times, IEnumerable<Uri> uris, IEnumerable<int?> optionalIds, IEnumerable<DateOnly?> optionalDates, IEnumerable<JsonObject> objects, IEnumerable<JsonArray> arrays) => new();
    }

    QueryParameters _result;

    void Establish()
    {
        _serviceProviderIsService.IsService(Arg.Any<Type>()).Returns(true);
        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query));
        _result = _performer.Parameters;
    }

    [Fact] void should_bind_dates() => _result.Any(p => p.Name == "dates" && p.Type == typeof(IEnumerable<DateOnly>)).ShouldBeTrue();
    [Fact] void should_bind_times() => _result.Any(p => p.Name == "times" && p.Type == typeof(IEnumerable<TimeOnly>)).ShouldBeTrue();
    [Fact] void should_bind_uris() => _result.Any(p => p.Name == "uris" && p.Type == typeof(IEnumerable<Uri>)).ShouldBeTrue();
    [Fact] void should_bind_nullable_integers() => _result.Any(p => p.Name == "optionalIds" && p.Type == typeof(IEnumerable<int?>)).ShouldBeTrue();
    [Fact] void should_bind_nullable_dates() => _result.Any(p => p.Name == "optionalDates" && p.Type == typeof(IEnumerable<DateOnly?>)).ShouldBeTrue();
    [Fact] void should_bind_json_objects() => _result.Any(p => p.Name == "objects" && p.Type == typeof(IEnumerable<JsonObject>)).ShouldBeTrue();
    [Fact] void should_bind_json_arrays() => _result.Any(p => p.Name == "arrays" && p.Type == typeof(IEnumerable<JsonArray>)).ShouldBeTrue();
    [Fact] void should_not_inject_any_collection() => _performer.Dependencies.ShouldBeEmpty();
}
