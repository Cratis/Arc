// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.ProxyGenerator.for_MethodInfoExtensions;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions;

public class when_selecting_metadata_loaded_native_union_cases : Specification
{
    IDisposable _owner;
    Type _methods;

    void Establish()
    {
        var errors = new List<string>();
        var assembly = typeof(when_selecting_native_union_responses).Assembly;
        Assert.True(TypeExtensions.TryInitializeProjectAssemblies(assembly.Location, _ => { }, errors.Add), string.Join(Environment.NewLine, errors));
        _owner = TypeExtensions.OwnProjectAssemblies();
        _methods = TypeExtensions.Assemblies.Single(candidate => candidate.FullName == assembly.FullName)
            .GetType(typeof(when_selecting_native_union_responses).FullName!)!;
    }

    void Destroy() => _owner.Dispose();

    [Fact] void should_suppress_handled_cases_without_runtime_assignability() => _methods.GetMethod("Event")!.GetResponseModel().HasResponse.ShouldBeFalse();
    [Fact] void should_select_the_client_case() => _methods.GetMethod("Reply")!.GetResponseModel().ResponseModel.Type.FullName.ShouldEqual("System.String");
    [Fact] void should_select_closed_generic_cases() => _methods.GetMethod("ClosedGeneric")!.GetResponseModel().ResponseModel.Type.FullName.ShouldEqual("System.String");
    [Fact] void should_select_tuple_elements() => _methods.GetMethod("Tuple")!.GetResponseModel().ResponseModel.Type.FullName.ShouldEqual("System.Guid");
    [Fact] void should_detect_bare_operation_collections() => _methods.GetMethod("Bare")!.ReturnType.ContainsBareCommandOperationCollection().ShouldBeTrue();
    [Fact] void should_preserve_controller_outputs() => _methods.GetMethod("Event")!.GetResponseModel(false).HasResponse.ShouldBeTrue();
}
#endif
