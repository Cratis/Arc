// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_MethodInfoExtensions.when_getting_a_response_model;

public class and_an_object_collection_is_returned : given.an_attributed_command_response
{
    void Because() => GenerateProxy(nameof(given.command_methods.ReturnsObjectCollection));

    [Fact] void should_preserve_the_response() => _result.HasResponse.ShouldBeTrue();
    [Fact] void should_preserve_the_collection() => _result.ResponseModel.IsEnumerable.ShouldBeTrue();
    [Fact] void should_use_the_object_element_type() => _result.ResponseModel.Type.ShouldEqual(typeof(object));
    [Fact] void should_generate_a_response_bearing_command() => _proxy.ShouldContain("extends Command<IRegisterProject,");
}
