// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ControllerObservableQueryAdapter.when_handling_a_subject;

public class and_the_interceptor_fails : given.a_request_with_emission_processing
{
    const string SecretMessage = "Confidential MVC interceptor failure detail";

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    async Task should_log_the_failure_and_honor_exception_detail_exposure(bool exposeExceptionDetails, bool asyncEnumerable)
    {
        _arcOptions.ExposeExceptionDetails = exposeExceptionDetails;
        var failure = new InterceptionFailed(SecretMessage);
        _interceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>()).Returns<Task<IEnumerable<object>>>(_ => throw failure);
        using var subject = new BehaviorSubject<TestData>(new("private"));
        await Handle(asyncEnumerable ? TwoItems() : subject);

        var result = (QueryResult)_result.Value!;
        _result.StatusCode.ShouldEqual(500);
        result.Data.ShouldBeNull();
        result.ExceptionMessages.ShouldContainOnly(exposeExceptionDetails ? SecretMessage : ExceptionDetailRedactor.RedactedMessage);
        result.ExceptionStackTrace.ShouldEqual(exposeExceptionDetails ? failure.StackTrace! : string.Empty);
        failure.StackTrace.ShouldNotBeEmpty();
        JsonSerializer.Serialize(result).Contains(SecretMessage, StringComparison.Ordinal).ShouldEqual(exposeExceptionDetails);
        _emissionLogger.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(ILogger.Log) && call.GetArguments()[3] == failure).ShouldBeTrue();
    }

    class InterceptionFailed(string message) : Exception(message);
}
