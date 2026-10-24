// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_ObservableQueryHttp.when_waiting_for_an_allowed_emission;

public class and_the_source_completes_without_an_eligible_emission : given.a_request
{
    Subject<string> _subject;

    void Establish()
    {
        _guards.Guard(Arg.Any<ObservableQueryEmissionContext>()).Returns(Task.FromResult(ObservableQueryEmissionVerdict.Suppress));
        _subject = new Subject<string>();
    }

    async Task Because()
    {
        var pending = ObservableQueryHttp.CreateResponse(_queryContext, _subject, new(true, TimeSpan.FromSeconds(2)), _request);
        _subject.OnNext("withheld");
        _subject.OnCompleted();
        _response = await pending;
    }

    void Destroy() => _subject.Dispose();

    [Fact]
    void should_report_completion_without_an_eligible_emission()
    {
        _response.StatusCode.ShouldEqual(HttpStatusCode.InternalServerError);
        _response.Result.Data.ShouldBeNull();
        _subject.HasObservers.ShouldBeFalse();
    }
}
