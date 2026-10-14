// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // The fixture uses multiline raw source snippets.

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;

static class DecisionAnalyzerSource
{
    internal const string Definitions = """
        using System;
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Keys;
        using Cratis.Chronicle.Projections;
        using Cratis.Chronicle.Projections.ModelBound;
        using Cratis.Chronicle.ReadModels;
        using Cratis.Chronicle.Reducers;
        [EventType]
        public record Changed;
        public record OwnerId(Guid Value) : EventSourceId<Guid>(Value);
        public record MemberId(Guid Value) : EventSourceId<Guid>(Value);
        [FromEvent<Changed>]
        public record State([property: Key] OwnerId Id);
        [FromEvent<Changed>]
        public record MemberState([property: Key] MemberId Id);
        [FromEvent<Changed>]
        public record RawState([property: Key] Guid Id);
        public record Plain(OwnerId Id);
        """;
}
