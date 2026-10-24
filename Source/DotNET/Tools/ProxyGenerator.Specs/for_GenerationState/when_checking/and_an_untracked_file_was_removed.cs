// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GenerationState.when_checking;

public class and_an_untracked_file_was_removed : given_a_saved_state
{
    void Establish()
    {
        var handwritten = Path.Combine(Folder, "handwritten.ts");
        File.WriteAllText(handwritten, "handwritten");
        GenerationState.Save(StateFile, Fingerprint, [Output]);
        File.Delete(handwritten);
    }
    void Because() => Result = GenerationState.IsUpToDate(StateFile, Fingerprint);
    [Fact] void should_only_check_recorded_outputs() => Result.ShouldBeTrue();
}
