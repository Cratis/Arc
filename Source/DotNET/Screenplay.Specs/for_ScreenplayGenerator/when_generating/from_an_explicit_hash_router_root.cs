// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_explicit_hash_router_root : given.a_screen_with_competing_routes
{
    void Establish()
    {
        Routes = """
            import { createHashRouter as router } from 'react-router-dom';
            import { AuthorDetails as Details } from './Authors/Details/AuthorDetails';
            export const app = router([{ path: '/details', element: <Details /> }]);
            """;
        ExtraFiles = [("Library/NestedRoutes.tsx", RouteImports + "\nexport const NestedRoutes = () => <Routes><Route path=\"other\" element={<Details />} /></Routes>;")];
    }

    void Because() => GenerateNavigation();

    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_prove_the_hash_router_factory_root_despite_another_table() => Result.Source.ShouldContain("item authorDetails navigate to Details.AuthorDetails");
    [Fact] void should_report_the_other_table_as_an_unknown_mount() => Result.Diagnostics.Count(_ => _.Code == "SP0028" && _.Message.Contains("unknown parent prefix", StringComparison.Ordinal)).ShouldEqual(1);
}
