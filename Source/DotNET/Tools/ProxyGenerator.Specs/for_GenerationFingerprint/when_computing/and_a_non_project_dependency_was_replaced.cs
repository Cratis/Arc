// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GenerationFingerprint.when_computing;

public class and_a_non_project_dependency_was_replaced : given_an_input
{
    string _stateFile;
    byte[] _originalDeps;
    byte[] _originalAssembly;
    bool _isUpToDate;

    void Establish()
    {
        _stateFile = Path.Combine(Folder, "state.json");
        var output = Path.Combine(Folder, "output.ts");
        File.WriteAllText(output, "output");
        GenerationState.Save(_stateFile, Original!, [output]);
        _originalDeps = File.ReadAllBytes(Path.ChangeExtension(AssemblyFile, ".deps.json"));
        _originalAssembly = File.ReadAllBytes(AssemblyFile);
        var dependency = Path.Combine(Folder, "Packaged.dll");
        var modified = File.GetLastWriteTimeUtc(dependency);
        File.WriteAllText(dependency, "Replaced.dll");
        File.SetLastWriteTimeUtc(dependency, modified);
    }

    void Because()
    {
        Result = GenerationFingerprint.Compute(AssemblyFile, Arguments, Header, Identity);
        _isUpToDate = GenerationState.IsUpToDate(_stateFile, Result!);
    }

    [Fact] void should_require_generation() => _isUpToDate.ShouldBeFalse();
    [Fact] void should_change_the_fingerprint() => Result.ShouldNotEqual(Original);
    [Fact] void should_still_compute_a_fingerprint() => Result.ShouldNotBeNull();
    [Fact] void should_leave_the_dependency_manifest_unchanged() => File.ReadAllBytes(Path.ChangeExtension(AssemblyFile, ".deps.json")).SequenceEqual(_originalDeps).ShouldBeTrue();
    [Fact] void should_leave_the_project_assembly_unchanged() => File.ReadAllBytes(AssemblyFile).SequenceEqual(_originalAssembly).ShouldBeTrue();
}
