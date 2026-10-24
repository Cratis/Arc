// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GenerationFingerprint.when_computing;

public class given_an_input : Specification, IDisposable
{
    protected string Folder;
    protected string AssemblyFile;
    protected string[] Arguments = ["assembly", "output"];
    protected string Header = "header";
    protected string Identity = "tool";
    protected string? Original;
    protected string? Result;

    void Establish()
    {
        Folder = Path.Combine(Path.GetTempPath(), $"arc-fingerprint-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Folder);
        AssemblyFile = Path.Combine(Folder, "Application.dll");
        foreach (var name in new[] { "Application.dll", "Application.pdb", "Application.xml", "Referenced.dll", "Packaged.dll", "Packaged.xml" })
        {
            File.WriteAllText(Path.Combine(Folder, name), name);
        }
        const string dependencies =
            "{ \"runtimeTarget\": { \"name\": \".NETCoreApp,Version=v10.0\" }, " +
            "\"targets\": { \".NETCoreApp,Version=v10.0\": { " +
            "\"Application/1.0.0\": { \"runtime\": { \"Application.dll\": {} } }, " +
            "\"Referenced/1.0.0\": { \"runtime\": { \"Referenced.dll\": {} } }, " +
            "\"Packaged/1.0.0\": { \"runtime\": { \"lib/net10.0/Packaged.dll\": {} } } } }, " +
            "\"libraries\": { " +
            "\"Application/1.0.0\": { \"type\": \"project\", \"serviceable\": false, \"sha512\": \"\" }, " +
            "\"Referenced/1.0.0\": { \"type\": \"project\", \"serviceable\": false, \"sha512\": \"\" }, " +
            "\"Packaged/1.0.0\": { \"type\": \"package\", \"serviceable\": false, \"sha512\": \"\", \"path\": \"packaged/1.0.0\" } } }";
        File.WriteAllText(Path.ChangeExtension(AssemblyFile, ".deps.json"), dependencies);
        Original = GenerationFingerprint.Compute(AssemblyFile, Arguments, Header, Identity);
    }

    public void Dispose() => Directory.Delete(Folder, true);
}
