// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_performing;

public class with_a_cancellation_token_parameter_on_an_aborted_request : given.a_model_bound_query_performer
{
    public record TestReadModel
    {
        public static bool ReceivedCancelledToken { get; set; }

        public static Task<TestReadModel> Query(CancellationToken cancellationToken)
        {
            ReceivedCancelledToken = cancellationToken.IsCancellationRequested;
            return Task.FromResult(new TestReadModel());
        }
    }

    void Establish()
    {
        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query));
        _context = _context with { CancellationToken = new CancellationToken(true) };
    }

    async Task Because() => await PerformQuery();

    [Fact] void should_hand_the_method_a_cancelled_token() => TestReadModel.ReceivedCancelledToken.ShouldBeTrue();
}
