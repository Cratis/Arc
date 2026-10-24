// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GenerationState.when_checking;

public class and_output_bytes_changed_without_metadata_changes : given_a_saved_state
{
    DateTime _originalTime;
    long _originalLength;

    void Establish()
    {
        _originalTime = File.GetLastWriteTimeUtc(Output);
        _originalLength = new FileInfo(Output).Length;
        File.WriteAllText(Output, "edited");
        File.SetLastWriteTimeUtc(Output, _originalTime);
    }

    void Because() => Result = GenerationState.IsUpToDate(StateFile, Fingerprint);

    [Fact] void should_invalidate_the_incremental_state() => Result.ShouldBeFalse();
    [Fact] void should_have_the_same_length() => new FileInfo(Output).Length.ShouldEqual(_originalLength);
    [Fact] void should_have_the_same_timestamp() => File.GetLastWriteTimeUtc(Output).ShouldEqual(_originalTime);
}
