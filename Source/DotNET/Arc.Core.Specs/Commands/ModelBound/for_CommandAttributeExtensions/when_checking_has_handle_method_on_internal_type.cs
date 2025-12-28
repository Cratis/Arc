// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.ModelBound.for_CommandAttributeExtensions;

public class when_checking_has_handle_method_on_internal_type : Specification
{
    bool _result;

    void Because() => _result = typeof(InternalCommand).HasHandleMethod();

    [Fact] void should_have_handle_method() => _result.ShouldBeTrue();
}
