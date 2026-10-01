// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_performing;

public class with_a_cancellation_token_parameter_and_no_token_in_the_context : given.a_model_bound_query_performer
{
    public record TestReadModel
    {
        public static CancellationToken? ReceivedToken { get; set; }

        public static TestReadModel Query(CancellationToken cancellationToken)
        {
            ReceivedToken = cancellationToken;
            return new TestReadModel();
        }
    }

    void Establish() => EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query));

    async Task Because() => await PerformQuery();

    [Fact] void should_not_fail_the_query() => TestReadModel.ReceivedToken.ShouldNotBeNull();
    [Fact] void should_hand_the_method_a_token_that_is_never_cancelled() => TestReadModel.ReceivedToken!.Value.CanBeCanceled.ShouldBeFalse();
}
