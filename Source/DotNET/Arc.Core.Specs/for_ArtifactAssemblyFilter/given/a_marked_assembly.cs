// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;

namespace Cratis.Arc.for_ArtifactAssemblyFilter.given;

public class a_marked_assembly : Specification
{
    protected Type _artifactType;

    void Establish()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"Artifacts{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
        assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(ArtifactsRequireOptInAttribute).GetConstructor(Type.EmptyTypes)!, []));
        _artifactType = assembly.DefineDynamicModule("Artifacts").DefineType("Artifact", TypeAttributes.Public).CreateType()!;
    }
}
