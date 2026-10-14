// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

public class a_screen_with_an_unknown_mount : a_screen_with_competing_routes
{
    protected string MountImport;

    void Establish()
    {
        Component = Component.Replace("'/details'", "'/team/details'", StringComparison.Ordinal);
        ExtraFiles = [("Library/NestedRoutes.tsx", RouteImports + "\nexport const NestedRoutes = () => <Routes><Route path=\"details\" element={<Details />} /></Routes>;")];
    }

    protected void GenerateUnknownMount()
    {
        Routes = RouteImports + "\nimport { BrowserRouter as Host } from 'react-router-dom';\n" + MountImport + """

            export const App = () => <Host><Routes>
                <Route path="/team/details" element={<Details />} />
                <Route path="/team/other" element={<Details />} />
                <Route path="/authors" element={<NestedRoutes />} />
            </Routes></Host>;
            """;
        GenerateNavigation();
    }
}
