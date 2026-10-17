// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryMethod;

public class when_validating_marked_queries
{
    [Theory]
    [InlineData("duplicate")]
    [InlineData("task")]
    [InlineData("private")]
    [InlineData("internal")]
    [InlineData("protected")]
    [InlineData("protected internal")]
    [InlineData("private protected")]
    [InlineData("owner")]
    [InlineData("self")]
    public void should_reject_invalid_declarations_before_selection(string kind)
    {
        var type = InvalidType(kind);
        Catch.Exception(() => ModelBoundQueryMethod.GetMethods(type, method => method.IsValidQueryFor(type))).ShouldBeOfExactType<ModelBoundQueryMethod.InvalidQueryDeclaration>();
    }

    static Type InvalidType(string kind)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"InvalidQuery_{Guid.NewGuid():N}"), AssemblyBuilderAccess.RunAndCollect);
        var type = assembly.DefineDynamicModule("Queries").DefineType("Invalid.ReadModel", TypeAttributes.Public);
        if (kind != "owner")
        {
            type.SetCustomAttribute(new CustomAttributeBuilder(typeof(ReadModelAttribute).GetConstructor(Type.EmptyTypes)!, [], [typeof(ReadModelAttribute).GetProperty(nameof(ReadModelAttribute.RequireQueryAttribute))!], [kind is not ("task" or "self")]));
        }

        var access = kind switch
        {
            "private" => MethodAttributes.Private,
            "internal" => MethodAttributes.Assembly,
            "protected" => MethodAttributes.Family,
            "protected internal" => MethodAttributes.FamORAssem,
            "private protected" => MethodAttributes.FamANDAssem,
            _ => MethodAttributes.Public
        };
        AddMethod(type, access, kind == "task" ? typeof(Task) : type, kind == "self" ? type : typeof(int));
        if (kind == "duplicate")
        {
            AddMethod(type, MethodAttributes.Public, type, typeof(string));
        }

        return type.CreateType()!;
    }

    static void AddMethod(TypeBuilder type, MethodAttributes access, Type result, Type parameter)
    {
        var method = type.DefineMethod("Query", access | MethodAttributes.Static, result, [parameter]);
        method.SetCustomAttribute(new CustomAttributeBuilder(typeof(QueryAttribute).GetConstructor(Type.EmptyTypes)!, []));
        var body = method.GetILGenerator();
        body.Emit(OpCodes.Ldnull);
        body.Emit(OpCodes.Ret);
    }
}
