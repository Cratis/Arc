// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ObservableQueryHttp.when_intercepting_an_emission;

public class and_an_async_enumerable_fails : given.a_request
{
    const string SecretMessage = "Confidential enumeration failure detail";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_log_and_redact_enumerator_failures_according_to_the_options(bool exposeExceptionDetails)
    {
        _arcOptions.ExposeExceptionDetails = exposeExceptionDetails;
        var failure = new EnumerationFailed(SecretMessage);
        _response = await ObservableQueryHttp.CreateResponse(_queryContext, FailingItems(failure), new(false, TimeSpan.FromSeconds(1)), _request);
        _response.StatusCode.ShouldEqual(HttpStatusCode.InternalServerError);
        _response.Result.Data.ShouldBeNull();
        _response.Result.ExceptionMessages.ShouldContainOnly(exposeExceptionDetails ? SecretMessage : ExceptionDetailRedactor.RedactedMessage);
        _response.Result.ExceptionStackTrace.ShouldEqual(exposeExceptionDetails ? failure.StackTrace! : string.Empty);
        failure.StackTrace.ShouldNotBeEmpty();
        _logger.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(ILogger.Log) && call.GetArguments()[3] == failure).ShouldBeTrue();
    }

    static async IAsyncEnumerable<string> FailingItems(Exception failure)
    {
        yield return "buffered";
        await Task.Yield();
        throw failure;
    }

    class EnumerationFailed(string message) : Exception(message);
}
