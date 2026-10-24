// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GenerationState.when_checking;

public class and_only_the_output_time_changed : given_a_saved_state
{
    void Establish() => File.SetLastWriteTimeUtc(Output, File.GetLastWriteTimeUtc(Output).AddMinutes(1));
    void Because() => Result = GenerationState.IsUpToDate(StateFile, Fingerprint);
    [Fact] void should_regenerate() => Result.ShouldBeFalse();
}
