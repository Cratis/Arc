// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GenerationState;

public class given_a_saved_state : Specification, IDisposable
{
    protected string Folder;
    protected string StateFile;
    protected string Output;
    protected string Fingerprint = "fingerprint";
    protected bool Result;

    void Establish()
    {
        Folder = Path.Combine(Path.GetTempPath(), $"arc-state-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Folder);
        StateFile = Path.Combine(Folder, "state.json");
        Output = Path.Combine(Folder, "output.ts");
        File.WriteAllText(Output, "output");
        GenerationState.Save(StateFile, Fingerprint, [Output]);
    }

    public void Dispose() => Directory.Delete(Folder, true);
}
