// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator;

public class when_project_assembly_initialization_fails : Specification
{
    string _invalidAssemblyFile;
    string _outputPath;
    bool _result;

    void Establish()
    {
        _invalidAssemblyFile = Path.Combine(Path.GetTempPath(), $"proxy-generator-invalid-{Guid.NewGuid():N}.dll");
        _outputPath = Path.Combine(Path.GetTempPath(), $"proxy-generator-output-{Guid.NewGuid():N}");
        File.WriteAllText(_invalidAssemblyFile, "not a managed assembly");
    }

    async Task Because() => _result = await Generator.Generate(_invalidAssemblyFile, _outputPath, 0, _ => { }, _ => { });

    void Destroy()
    {
        File.Delete(_invalidAssemblyFile);
        if (Directory.Exists(_outputPath))
        {
            Directory.Delete(_outputPath, true);
        }
    }

    [Fact] void should_stop_generation() => _result.ShouldBeFalse();
    [Fact] void should_not_write_any_proxy_files() => Directory.GetFiles(_outputPath, "*", SearchOption.AllDirectories).ShouldBeEmpty();
}
