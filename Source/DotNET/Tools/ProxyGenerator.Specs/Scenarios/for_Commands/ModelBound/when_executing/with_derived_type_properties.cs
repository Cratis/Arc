// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound.when_executing;

[Collection(ScenarioCollectionDefinition.Name)]
public class with_derived_type_properties : given.a_scenario_web_application
{
    CommandResult<DerivedTypeCommandResult>? _result;

    void Establish() => LoadCommandProxy<DerivedTypeCommand>();

    async Task Because()
    {
        var executionResult = await Bridge!.ExecuteCommandViaProxyAsync<DerivedTypeCommandResult>(new DerivedTypeCommand
        {
            Shape1 = new CircleShape { Label = "Circle", Radius = 5.0 },
            Shape2 = new RectangleShape { Label = "Rectangle", Width = 10.0, Height = 20.0 }
        });

        _result = executionResult.Result;
    }

    [Fact] void should_return_successful_result() => _result!.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_response() => _result!.Response.ShouldNotBeNull();
    [Fact] void should_deserialize_shape1_as_circle_shape() => _result!.Response!.Shape1TypeName.ShouldEqual("CircleShape");
    [Fact] void should_deserialize_shape2_as_rectangle_shape() => _result!.Response!.Shape2TypeName.ShouldEqual("RectangleShape");
    [Fact] void should_have_correct_circle_radius() => _result!.Response!.Shape1Radius.ShouldEqual(5.0);
    [Fact] void should_have_correct_rectangle_width() => _result!.Response!.Shape2Width.ShouldEqual(10.0);
    [Fact] void should_have_correct_rectangle_height() => _result!.Response!.Shape2Height.ShouldEqual(20.0);
}
