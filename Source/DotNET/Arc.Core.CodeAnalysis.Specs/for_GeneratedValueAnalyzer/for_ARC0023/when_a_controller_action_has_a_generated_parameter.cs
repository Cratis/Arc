// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0023;

public class when_a_controller_action_has_a_generated_parameter : given.a_generated_value_analyzer
{
    [Theory]
    [InlineData("", ": Microsoft.AspNetCore.Mvc.ControllerBase", "Handle")]
    [InlineData("", ": Microsoft.AspNetCore.Mvc.ControllerBase", "Action")]
    [InlineData("[Microsoft.AspNetCore.Mvc.ApiController]", "", "Handle")]
    [InlineData("[Microsoft.AspNetCore.Mvc.ApiController]", "", "Action")]
    [InlineData("[Microsoft.AspNetCore.Mvc.Controller]", "", "Handle")]
    [InlineData("[Microsoft.AspNetCore.Mvc.Controller]", "", "Action")]
    [InlineData("", ": Parent", "Handle")]
    public async Task should_report_controller_methods_regardless_of_their_name(string attribute, string inheritance, string method) => await VerifyDiagnostic(
        $@"
        namespace Microsoft.AspNetCore.Mvc
        {{
            public abstract class ControllerBase {{ }}
            public class ApiControllerAttribute : Attribute {{ }}
            public class ControllerAttribute : Attribute {{ }}
        }}
        [Microsoft.AspNetCore.Mvc.ApiController] public class Parent {{ }}
        {attribute}
        public class Example {inheritance}
        {{
            public void {method}([{{|#0:GeneratedValue|}}] CartId value) {{ }}
        }}",
        "ARC0023",
        "value");
}
