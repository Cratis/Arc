// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_validating_arguments;

public class with_null_value_for_optional_parameter : given.a_model_bound_query_performer
{
    public record TestReadModel
    {
        public static int ReceivedValue { get; set; }

        public static TestReadModel Query(int optionalInt = 42)
        {
            ReceivedValue = optionalInt;
            return new TestReadModel();
        }
    }

    void Establish()
    {
        var parameters = new QueryArguments();
        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query), parameters: parameters);
    }

    async Task Because() => await PerformQuery();

    [Fact] void should_use_default_value() => TestReadModel.ReceivedValue.ShouldEqual(42);
}
