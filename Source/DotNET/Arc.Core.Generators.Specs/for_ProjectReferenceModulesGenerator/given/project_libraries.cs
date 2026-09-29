// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.Testing;

namespace Cratis.Arc.Generators.Specs.for_ProjectReferenceModulesGenerator.given;

public class project_libraries : Specification
{
    protected const string ModuleInitializerRanKey = "Cratis.Arc.Generators.Specs.VisibleLibrary.ModuleInitializerRan";

    protected ProjectLibrary _visibleLibrary;
    protected ProjectLibrary _hiddenLibrary;
    protected ProjectLibrary _packageLibrary;

    void Establish()
    {
        const string visibleSource = $$"""
            namespace VisibleLibrary.Things
            {
                public class Visible;

                static class Registration
                {
                    [System.Runtime.CompilerServices.ModuleInitializer]
                    internal static void Register() => System.AppContext.SetData("{{ModuleInitializerRanKey}}", true);
                }
            }
            """;
        _visibleLibrary = ProjectLibrary.Compile("VisibleLibrary", visibleSource);
        _hiddenLibrary = ProjectLibrary.Compile("HiddenLibrary", "namespace HiddenLibrary { internal class Hidden; }");
        _packageLibrary = ProjectLibrary.Compile("PackageLibrary", "namespace PackageLibrary { public class Packaged; }");
    }
}
