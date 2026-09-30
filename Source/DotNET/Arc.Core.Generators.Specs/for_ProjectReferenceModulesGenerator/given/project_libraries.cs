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
    protected ProjectLibrary _executableLibrary;
    protected ProjectLibrary _fileLocalLibrary;

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

        // A referenced executable that lets the executable under compilation see its internals, as a web application
        // does for its test project. What it declares is what Arc generates into every executable.
        const string ExecutableSource = """
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("App")]
            namespace Cratis.Arc.Generated { internal static class __CratisArcProjectReferenceModules { } }
            namespace Cratis.Arc.Commands.Generated { internal static class OperationRegistration { } }
            """;
        _executableLibrary = ProjectLibrary.Compile("ExecutableLibrary", ExecutableSource);

        // A library a source generator has added a file-local type to, as Fundamentals' type discovery generator does
        // to every project. It lets the executable see its internals, so only the file-local type's own rules keep it
        // from being named. Its metadata name starts with '<', so it sorts before every other type.
        const string FileLocalSource = """
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("App")]
            namespace Cratis.Types.Generated { file static class GeneratedTypeDiscoveryProviderRegistration { } }
            namespace FileLocalLibrary
            {
                file static class Registration { }
                public class Visible;
            }
            """;
        _fileLocalLibrary = ProjectLibrary.Compile("FileLocalLibrary", FileLocalSource);
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

    protected static ProjectLibrary CompileWithThrowingModuleInitializer(string name, string types)
    {
        var source = $$"""
            namespace {{name}}
            {
                {{types}}

                static class Registration
                {
                    [System.Runtime.CompilerServices.ModuleInitializer]
                    internal static void Register() => throw new System.InvalidOperationException("The module initializer failed");
                }
            }
            """;
        return ProjectLibrary.Compile(name, source);
    }
}
