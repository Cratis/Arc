// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc.for_HostBuilderExtensions.given;

/// <summary>
/// Builds real hosts through AddCratisArcCore that see an assembly marked with <see cref="ArtifactsRequireOptInAttribute"/>.
/// </summary>
public static class MarkedArtifactHosts
{
    /// <summary>
    /// Creates a marked assembly holding one model-bound command and one read model with a query.
    /// </summary>
    /// <returns>The assembly, the command type and the read model type.</returns>
    public static (Assembly Assembly, Type Command, Type ReadModel) CreateMarkedAssembly()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"MarkedArtifacts{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
        assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(ArtifactsRequireOptInAttribute).GetConstructor(Type.EmptyTypes)!, []));
        var module = assembly.DefineDynamicModule("MarkedArtifacts");

        var command = module.DefineType("MarkedCommand", TypeAttributes.Public);
        command.SetCustomAttribute(new CustomAttributeBuilder(typeof(CommandAttribute).GetConstructor(Type.EmptyTypes)!, []));
        command.DefineDefaultConstructor(MethodAttributes.Public);
        EmitReturn(command.DefineMethod("Handle", MethodAttributes.Public, typeof(void), Type.EmptyTypes), false);

        var readModel = module.DefineType("MarkedReadModel", TypeAttributes.Public);
        readModel.SetCustomAttribute(new CustomAttributeBuilder(typeof(ReadModelAttribute).GetConstructor(Type.EmptyTypes)!, []));
        readModel.DefineDefaultConstructor(MethodAttributes.Public);
        EmitReturn(readModel.DefineMethod("GetById", MethodAttributes.Public | MethodAttributes.Static, readModel, [typeof(Guid)]), true);

        var commandType = command.CreateType()!;

        // The created type reports the runtime assembly, which is not the AssemblyBuilder instance.
        return (commandType.Assembly, commandType, readModel.CreateType()!);
    }

    /// <summary>
    /// Builds a service provider through the production registration path, where the type universe holds the given types.
    /// </summary>
    /// <param name="types">The types the host discovers.</param>
    /// <param name="included">The assembly the host opts into, if any.</param>
    /// <returns>The built <see cref="ServiceProvider"/>.</returns>
    public static ServiceProvider Build(Type[] types, Assembly? included)
    {
        var services = new ServiceCollection().AddCratisArcCore();

        // Layer the types over the host's own universe, so provider discovery keeps working.
        using (var discovery = services.BuildServiceProvider())
        {
            services.Replace(ServiceDescriptor.Singleton<ITypes>(new TypesWithExtras(discovery.GetRequiredService<ITypes>(), types)));
        }

        if (included is not null)
        {
            services.Configure<ArcOptions>(options => options.Artifacts.Include(included));
        }

        return services.BuildServiceProvider();
    }

    static void EmitReturn(MethodBuilder method, bool returnsValue)
    {
        var il = method.GetILGenerator();
        if (returnsValue)
        {
            il.Emit(OpCodes.Ldnull);
        }

        il.Emit(OpCodes.Ret);
    }
}
