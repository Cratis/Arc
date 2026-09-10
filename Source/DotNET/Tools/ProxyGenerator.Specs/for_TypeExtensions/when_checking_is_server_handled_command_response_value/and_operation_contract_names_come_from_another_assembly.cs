// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_checking_is_server_handled_command_response_value;

public class and_operation_contract_names_come_from_another_assembly : Specification
{
    bool _markerIsHandled;
    bool _batchIsHandled;
    bool _collectionIsAnOperationBatch;

    void Because()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"OperationCollision.{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("Main");
        var marker = module.DefineType("Cratis.Arc.Commands.ICommandOperation", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract).CreateType();
        var batch = module.DefineType("Cratis.Arc.Commands.CommandOperations", TypeAttributes.Public).CreateType();
        _markerIsHandled = marker.IsServerHandledCommandResponseValue();
        _batchIsHandled = batch.IsServerHandledCommandResponseValue();
        _collectionIsAnOperationBatch = marker.MakeArrayType().ContainsBareCommandOperationCollection();
    }

    [Fact] void should_not_hide_the_lookalike_marker_response() => _markerIsHandled.ShouldBeFalse();
    [Fact] void should_not_hide_the_lookalike_batch_response() => _batchIsHandled.ShouldBeFalse();
    [Fact] void should_leave_the_lookalike_collection_as_response_data() => _collectionIsAnOperationBatch.ShouldBeFalse();
}
