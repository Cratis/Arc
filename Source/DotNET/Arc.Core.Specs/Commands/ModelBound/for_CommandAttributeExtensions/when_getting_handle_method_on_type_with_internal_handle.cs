// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Commands.ModelBound.for_CommandAttributeExtensions;

public class when_getting_handle_method_on_type_with_internal_handle : Specification
{
    MethodInfo _result;

    void Because() => _result = typeof(PublicCommandWithInternalHandle).GetHandleMethod();

    [Fact] void should_get_handle_method() => _result.ShouldNotBeNull();
    [Fact] void should_get_method_named_handle() => _result.Name.ShouldEqual("Handle");
}
