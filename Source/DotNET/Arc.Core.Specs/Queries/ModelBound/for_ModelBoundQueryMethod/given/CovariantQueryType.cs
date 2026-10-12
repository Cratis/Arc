// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryMethod.given;

/// <summary>
/// Builds a covariant query fixture without applying a compiler reader's different conventional return rules.
/// </summary>
public static class CovariantQueryType
{
    /// <summary>
    /// Creates a read model returning a collection of a derived model.
    /// </summary>
    /// <param name="explicitMode">Whether the owner requires query markers.</param>
    /// <returns>The read-model type.</returns>
    public static Type Create(bool explicitMode)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"CovariantQuery_{Guid.NewGuid():N}"), AssemblyBuilderAccess.RunAndCollect);
        var module = assembly.DefineDynamicModule("Queries");
        var owner = module.DefineType("Covariant.ReadModel", TypeAttributes.Public);
        var derived = module.DefineType("Covariant.Derived", TypeAttributes.Public, owner);
        owner.SetCustomAttribute(new CustomAttributeBuilder(typeof(ReadModelAttribute).GetConstructor(Type.EmptyTypes)!, [], [typeof(ReadModelAttribute).GetProperty(nameof(ReadModelAttribute.RequireQueryAttribute))!], [explicitMode]));
        var method = owner.DefineMethod("All", MethodAttributes.Public | MethodAttributes.Static, typeof(IEnumerable<>).MakeGenericType(derived), Type.EmptyTypes);
        if (explicitMode)
        {
            method.SetCustomAttribute(new CustomAttributeBuilder(typeof(QueryAttribute).GetConstructor(Type.EmptyTypes)!, []));
        }

        var body = method.GetILGenerator();
        body.Emit(OpCodes.Ldnull);
        body.Emit(OpCodes.Ret);
        var type = owner.CreateType()!;
        derived.CreateType();
        return type;
    }
}
