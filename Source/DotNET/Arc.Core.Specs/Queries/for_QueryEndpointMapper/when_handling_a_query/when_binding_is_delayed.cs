// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_handling_a_query;

public class when_binding_is_delayed : given.a_query_request
{
    readonly DateTimeOffset _received = new(2026, 6, 7, 8, 9, 10, TimeSpan.Zero);
    DateTimeOffset? _observed;
    DateTimeOffset? _later;
    bool _bindingRan;

    void Establish()
    {
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(_received);
        var originalServices = _context.RequestServices;
        var services = Substitute.For<IServiceProvider>();
        services.GetService(Arg.Any<Type>()).Returns(call => call.Arg<Type>() == typeof(TimeProvider)
            ? clock
            : originalServices.GetService(call.Arg<Type>()));
        _context.RequestServices.Returns(services);
        _context.Query.Returns(_ =>
        {
            _bindingRan = true;
            _later = _received.AddMinutes(1);
            clock.GetUtcNow().Returns(_later.Value);
            return new Dictionary<string, string>();
        });
        _queryPipeline.Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>())
            .Returns(_ =>
            {
                _observed = new OperationContextAccessor().ReceivedAt;
                return Task.FromResult(QueryResult.Success(CorrelationId.New()));
            });
    }

    async Task Because() => await _mapper.HandlerFor("GET")(_context);

    [Fact] void should_bind_after_capturing_the_receipt() => _bindingRan.ShouldBeTrue();
    [Fact] void should_keep_the_receipt_captured_before_binding() => _observed.ShouldEqual(_received);
    [Fact] void should_advance_the_clock_during_binding() => _later.ShouldNotEqual(_observed);
}
