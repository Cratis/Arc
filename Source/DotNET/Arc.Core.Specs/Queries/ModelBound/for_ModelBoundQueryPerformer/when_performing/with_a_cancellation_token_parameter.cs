// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_performing;

public class with_a_cancellation_token_parameter : given.a_model_bound_query_performer
{
    public record TestReadModel
    {
        public static object? ReceivedDependency { get; set; }
        public static CancellationToken ReceivedToken { get; set; }
        public static string ReceivedName { get; set; } = string.Empty;

        public static TestReadModel Query(object dependency, string name, CancellationToken cancellationToken)
        {
            ReceivedDependency = dependency;
            ReceivedToken = cancellationToken;
            ReceivedName = name;
            return new TestReadModel();
        }
    }

    readonly object _expectedDependency = new();
    readonly CancellationTokenSource _requestAborted = new();

    void Establish()
    {
        _serviceProviderIsService.IsService(typeof(object)).Returns(true);
        _serviceProviderIsService.IsService(typeof(string)).Returns(false);

        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query), [_expectedDependency], new QueryArguments { ["name"] = "Token Example" });
        _context = _context with { CancellationToken = _requestAborted.Token };
    }

    async Task Because() => await PerformQuery();

    [Fact] void should_bind_the_token_to_the_requests_abort_token() => TestReadModel.ReceivedToken.ShouldEqual(_requestAborted.Token);
    [Fact] void should_not_have_a_token_that_is_already_cancelled() => TestReadModel.ReceivedToken.IsCancellationRequested.ShouldBeFalse();
    [Fact] void should_still_inject_the_dependency() => TestReadModel.ReceivedDependency.ShouldEqual(_expectedDependency);
    [Fact] void should_still_bind_the_arguments_before_it() => TestReadModel.ReceivedName.ShouldEqual("Token Example");

    void Destroy() => _requestAborted.Dispose();
}
