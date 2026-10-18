// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FileHeader.given;

public class a_project : Specification
{
    protected string _root = null!;
    protected string _project = null!;
    protected string _result = null!;

    void Establish()
    {
        _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _project = Path.Combine(_root, "Project");
        Directory.CreateDirectory(_project);
        File.WriteAllText(Path.Combine(_root, ".editorconfig"), "root = true\n[*.cs]\nfile_header_template = Parent header");
    }

    protected void ConfigureProject(string content) => File.WriteAllText(Path.Combine(_project, ".editorconfig"), content);

    void Destroy() => Directory.Delete(_root, true);
}
