// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_proven_root_with_a_disjoint_unknown_mount : given.a_screen_with_an_unknown_mount
{
    void Establish()
    {
        MountImport = "import { NestedRoutes } from '@routes/NestedRoutes';";
        Component = Component.Replace("'/team/details'", "'/team/other'", StringComparison.Ordinal);
    }

    void Because() => GenerateUnknownMount();

    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_map_the_browser_router_root_outside_the_unknown_suffix() => Result.Source.ShouldContain("item authorDetails navigate to Details.AuthorDetails");
    [Fact] void should_report_only_the_other_table_as_an_unknown_mount() => Result.Diagnostics.Count(_ => _.Code == "SP0028" && _.Message.Contains("unknown parent prefix", StringComparison.Ordinal)).ShouldEqual(1);
}
