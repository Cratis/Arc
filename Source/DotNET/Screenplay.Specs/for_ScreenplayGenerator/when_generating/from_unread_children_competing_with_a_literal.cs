// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_unread_children_competing_with_a_literal : given.a_screen_with_competing_routes
{
    void Establish() => Routes = """
        import { createBrowserRouter } from 'react-router-dom';
        import { AuthorDetails as Details } from './Authors/Details/AuthorDetails';
        export const router = createBrowserRouter([
            { path: '/', children: childRoutes },
            { path: '/details', element: <Details /> }
        ]);
        """;

    void Because() => GenerateNavigation();

    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_not_drop_the_unread_children() => Result.Source.Contains("navigate to", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_the_unread_table_once() => Result.Diagnostics.Count(_ => _.Code == "SP0028" && _.Location == RouteFile).ShouldEqual(1);
}
