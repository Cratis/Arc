// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.ReadModels.for_SelfReducingReadModel;

public class when_recognizing_self_reducers : Specification
{
    Type[] _recognized;

    void Because() => _recognized = new[] { typeof(Sync), typeof(Async), typeof(Oblivious), typeof(DerivedEventModel), typeof(NonNullable), typeof(WrongEvent), typeof(WrongContext), typeof(Optional), typeof(Generic<>), typeof(Abstract) }
        .Where(SelfReducingReadModel.IsSelfReducing).ToArray();

    [Fact] void should_accept_only_eligible_self_shapes() => _recognized.ShouldContainOnly(typeof(Sync), typeof(Async), typeof(Oblivious), typeof(DerivedEventModel));

    [EventType] public record Added;
    public record DerivedAdded : Added;

    record Sync
    {
        public static Sync? Reduce(Added @event, Sync? current) => current;
    }

    record Async
    {
        public static Task<Async?> Reduce(Added @event, Async? current, EventContext context) => Task.FromResult(current);
    }

    record Oblivious
    {
#nullable disable
        public static Oblivious Reduce(Added @event, Oblivious current) => current;
#nullable restore
    }

    record DerivedEventModel
    {
        public static DerivedEventModel Reduce(DerivedAdded @event, DerivedEventModel? current) => new();
    }

#pragma warning disable CHR0051, CHR0058 // Intentionally invalid self-reducer shapes, to specify which shapes are not recognized.
    record NonNullable
    {
        public static NonNullable Reduce(Added @event, NonNullable current) => current;
    }

    record WrongEvent
    {
        public static WrongEvent Reduce(object @event, WrongEvent? current) => new();
    }

    record WrongContext
    {
        public static WrongContext Reduce(Added @event, WrongContext? current, object context) => new();
    }

    record Optional
    {
        public static Optional Reduce(Added @event, Optional? current = null) => new();
    }
#pragma warning restore CHR0051, CHR0058

    record Generic<T>
    {
        public static Generic<T> Reduce(Added @event, Generic<T>? current) => new();
    }

    abstract record Abstract
    {
        public static Abstract? Reduce(Added @event, Abstract? current) => current;
    }
}
