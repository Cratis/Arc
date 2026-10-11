// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_a_fixture_pins_the_value : given.a_generated_value_resolver
{
    readonly Id _pinned = new(new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11));
    IGeneratedValueFixture _fixture;

    void Establish()
    {
        _fixture = Substitute.For<IGeneratedValueFixture>();
        _fixture.TryGet(typeof(CreatesId), "id", typeof(Id), out Arg.Any<object>()).Returns(call =>
        {
            call[3] = _pinned;
            return true;
        });
        _serviceProvider.GetService(typeof(IGeneratedValueFixture)).Returns(_fixture);
    }

    async Task Because() => _resolution = await Resolve();

    [Fact] void should_use_the_pinned_value() => _resolution.Arguments.ShouldContainOnly(_pinned);
    [Fact] void should_capture_the_pinned_value() => _context.GeneratedValues.Get<Id>("id").ShouldEqual(_pinned);
    [Fact] void should_record_the_capture_on_the_fixture() => _fixture.Received(1).Record(typeof(CreatesId), _context.GeneratedValues.Single());
}
