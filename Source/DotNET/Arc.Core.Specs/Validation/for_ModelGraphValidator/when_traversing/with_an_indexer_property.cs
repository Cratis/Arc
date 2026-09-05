// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation.for_ModelGraphValidator.when_traversing;

/// <summary>
/// An indexer needs index arguments, so reading it without any throws "Parameter count mismatch". Types carrying one
/// show up in an object-typed graph, so the traversal has to skip them rather than fall over.
/// </summary>
public class with_an_indexer_property : given.a_model_graph_validator
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() =>
        _validator.Validate(new ModelGraphValidationRequest(new ModelWithIndexer())));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_still_consider_the_model() => _typesAskedFor.ShouldContain(typeof(ModelWithIndexer));

    class ModelWithIndexer
    {
        public string this[int index] => index.ToString();

        public string Name => "some name";
    }
}
