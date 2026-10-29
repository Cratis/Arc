// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_pipeline_returns_an_exception_with_details_hidden : given.a_guarded_sse_connection
{
    void Establish()
    {
        _arcOptions.Value.ExposeExceptionDetails = false;
        _queryPipeline.Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(new QueryResult { ExceptionMessages = ["Query failed"], ExceptionStackTrace = "Internal stack" });
    }

    Task Because() => RunConnection(() => WaitFor(() => HasErrorFor(FirstQueryId)));

    [Fact] void should_hide_the_pipeline_error() => HubMessages.Single(message => message.Type == ObservableQueryHubMessageType.Error && message.QueryId == FirstQueryId)
        .Payload!.ToString().ShouldEqual(ExceptionDetailRedactor.RedactedMessage);
}
