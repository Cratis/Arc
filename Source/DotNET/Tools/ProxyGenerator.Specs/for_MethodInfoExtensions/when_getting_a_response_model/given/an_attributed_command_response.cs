// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_MethodInfoExtensions.when_getting_a_response_model.given;

public class an_attributed_command_response : Specification
{
    protected (bool HasResponse, ModelDescriptor ResponseModel) _result;
    protected string _proxy;

    protected void GenerateProxy(string methodName)
    {
        _result = typeof(command_methods).GetMethod(methodName)!.GetResponseModel();
        _proxy = TemplateTypes.Command(new
        {
            Name = "RegisterProject",
            Route = "api/projects/register",
            _result.HasResponse,
            ResponseType = _result.ResponseModel,
            Properties = Array.Empty<object>(),
            Parameters = Array.Empty<object>(),
            Imports = Array.Empty<object>(),
            Roles = Array.Empty<string>(),
            TreatWarningsAsErrorsForPolicy = false
        });
    }
}
