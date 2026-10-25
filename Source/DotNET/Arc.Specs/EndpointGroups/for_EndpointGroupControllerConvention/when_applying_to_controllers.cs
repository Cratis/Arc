// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.AspNetCore.EndpointGroups.for_EndpointGroupControllerConvention;

public class when_applying_to_controllers : given.a_grouped_and_an_ungrouped_controller
{
    [Fact] void should_put_the_grouped_controller_in_its_group() => _groupedEndpoint.Metadata.GetMetadata<EndpointGroupMetadata>()!.Name.ShouldEqual("internal");
    [Fact] void should_hide_the_grouped_controller_from_api_descriptions() => _groupedAction.ApiExplorer.IsVisible.ShouldEqual(false);
    [Fact] void should_leave_the_ungrouped_controller_without_a_group() => _ungroupedEndpoint.Metadata.GetMetadata<EndpointGroupMetadata>().ShouldBeNull();
    [Fact] void should_leave_the_ungrouped_controller_visible() => _ungroupedAction.ApiExplorer.IsVisible.ShouldBeNull();
}
