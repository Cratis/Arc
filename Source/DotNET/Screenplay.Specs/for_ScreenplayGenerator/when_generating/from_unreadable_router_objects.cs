// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_unreadable_router_objects : given.a_screen_with_routes
{
    void Establish()
    {
        Routes = """
            import { createBrowserRouter } from 'react-router-dom';
            import { AuthorDetails as Details } from './Authors/Details/AuthorDetails';
            export const router = createBrowserRouter([
                { path: '/lazy', lazy: () => import('./Authors/Details/AuthorDetails'), element: <Details /> },
                { path: routePath, element: <Details /> },
                { path: '/computed', element: renderDetails() },
                { path: '/spread', ...routeOptions },
                { path: '/children', children: routes }
            ]);
            """;
        Component = Imports + """

            export const AuthorRetirement = () => {
                const navigate = useNavigate();
                return <CommandDialog command={RetireAuthor} onSuccess={() => navigate('/lazy')} />;
            };
            """;
    }

    void Because() => GenerateNavigation();

    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_not_map_a_lazy_element_even_with_a_readable_element_property() => Result.Source.Contains("navigate to", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_all_route_omissions_once() => Result.Diagnostics.Count(_ => _.Code == "SP0028" && _.Location == RouteFile).ShouldEqual(1);
    [Fact] void should_report_lazy_loading() => Result.Diagnostics.Single(_ => _.Location == RouteFile).Message.ShouldContain("lazy route '/lazy'");
    [Fact] void should_report_computed_paths() => Result.Diagnostics.Single(_ => _.Location == RouteFile).Message.ShouldContain("routePath");
    [Fact] void should_report_computed_elements() => Result.Diagnostics.Single(_ => _.Location == RouteFile).Message.ShouldContain("renderDetails()");
    [Fact] void should_report_spreads_and_computed_children() => Result.Diagnostics.Single(_ => _.Location == RouteFile).Message.ShouldContain("computed or spread route declarations or children");
}
