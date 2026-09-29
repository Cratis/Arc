// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.Testing;

namespace Cratis.Arc.Generators.Specs.for_ProjectReferenceModulesGenerator.given;

public class project_libraries : Specification
{
    protected ProjectLibrary _visibleLibrary;
    protected ProjectLibrary _hiddenLibrary;
    protected ProjectLibrary _packageLibrary;
    protected ProjectLibrary _choosingLibrary;
    protected ProjectLibrary _compileOnlyLibrary;
    protected ProjectLibrary _derivedLibrary;

    void Establish()
    {
        _visibleLibrary = ProjectLibrary.Compile("VisibleLibrary", "namespace VisibleLibrary.Things { public class Visible; }");
        _hiddenLibrary = ProjectLibrary.Compile("HiddenLibrary", "namespace HiddenLibrary { internal class Hidden; }");
        _packageLibrary = ProjectLibrary.Compile("PackageLibrary", "namespace PackageLibrary { public class Packaged; }");
        _choosingLibrary = ProjectLibrary.Compile(
            "ChoosingLibrary",
            "namespace ChoosingLibrary { public class AImplementsAnInterface : System.IDisposable { public void Dispose() { } } public class WithoutDependencies; }");
        _compileOnlyLibrary = ProjectLibrary.Compile("CompileOnlyLibrary", "namespace CompileOnlyLibrary { public class Base; }");
        _derivedLibrary = ProjectLibrary.Compile(
            "DerivedLibrary",
            "namespace DerivedLibrary { public class Derived : CompileOnlyLibrary.Base; }",
            _compileOnlyLibrary);
    }

    protected static ProjectLibrary CompileWithModuleInitializer(string name, string types, string moduleInitializerRanKey, params ProjectLibrary[] references)
    {
        var source = $$"""
            namespace {{name}}
            {
                {{types}}

                static class Registration
                {
                    [System.Runtime.CompilerServices.ModuleInitializer]
                    internal static void Register() => System.AppContext.SetData("{{moduleInitializerRanKey}}", true);
                }
            }
            """;
        return ProjectLibrary.Compile(name, source, references);
    }
}
