// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_commented_import_of_nested_routes : given.a_screen_with_an_unknown_mount
{
    void Establish() => MountImport = "import { NestedRoutes } from /* module */ './NestedRoutes';";

    void Because() => GenerateUnknownMount();

    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_not_depend_on_import_comment_syntax() => Result.Source.Contains("navigate to", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_the_unknown_mount_once() => Result.Diagnostics.Count(_ => _.Code == "SP0028" && _.Location == "Library/NestedRoutes.tsx" && _.Message.Contains("unknown parent prefix", StringComparison.Ordinal)).ShouldEqual(1);
}
