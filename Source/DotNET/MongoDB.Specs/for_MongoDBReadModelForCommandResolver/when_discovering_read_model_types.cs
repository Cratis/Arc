// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_MongoDBReadModelForCommandResolver;

public class when_discovering_read_model_types : Specification
{
    IEnumerable<Type> _result;

    void Because() => _result = MongoDBReadModelForCommandResolver.DiscoverReadModelTypes(
        [typeof(Customer), typeof(Account), typeof(NotAReadModel), typeof(AnAbstractReadModel)]);

    [Fact] void should_include_the_read_models() => _result.ShouldContain(typeof(Customer), typeof(Account));
    [Fact] void should_not_include_a_type_that_is_not_a_read_model() => _result.ShouldNotContain(typeof(NotAReadModel));
    [Fact] void should_not_include_an_abstract_read_model() => _result.ShouldNotContain(typeof(AnAbstractReadModel));
}
