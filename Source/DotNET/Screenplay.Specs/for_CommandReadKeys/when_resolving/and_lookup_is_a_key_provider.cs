// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

namespace Cratis.Arc.Screenplay.for_CommandReadKeys.when_resolving;

public class and_lookup_is_a_key_provider : a_protected_document
{
    void Because() => Generate(Library("""
        [Command, ReadProtection(ReadProtectionMode.On)]
        public record BookRoom(RoomId RoomId, int Guests)
        {
            public Result<RoomBooked, ValidationResult> Handle([LookUpBy<CallerSubject>] RoomOccupancy occupancy) => new RoomBooked(Guests, occupancy.FreeBeds);
        }
        """));

    [Fact] void should_leave_the_protected_read_in_code() => Result.Source.Contains("reads RoomOccupancy", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_explain_the_provider_key() => Result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("key comes from key provider", StringComparison.Ordinal)).ShouldBeTrue();
}
