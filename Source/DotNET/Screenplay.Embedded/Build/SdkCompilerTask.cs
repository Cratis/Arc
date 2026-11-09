// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Embedded.Build;

/// <summary>
/// Runs the entire generation task on the consuming SDK's compiler when the bundled compiler is older.
/// </summary>
public static class SdkCompilerTask
{
    /// <summary>
    /// Determines whether generation needs the SDK's compiler instead of the bundled one.
    /// </summary>
    /// <param name="sdkCompiler">The SDK compiler version.</param>
    /// <param name="bundledCompiler">The bundled compiler version.</param>
    /// <returns>True only when the SDK compiler is newer.</returns>
    public static bool IsNewer(Version sdkCompiler, Version bundledCompiler) => sdkCompiler > bundledCompiler;

    /// <summary>
    /// Executes an isolated copy of the task if the SDK compiler is newer.
    /// </summary>
    /// <param name="task">The task and its MSBuild inputs.</param>
    /// <param name="documents">The resources returned by the isolated task.</param>
    /// <returns>The task result, or null to use the existing generation path.</returns>
    public static bool? Execute(GenerateEmbeddedScreenplayDocuments task, out ITaskItem[] documents)
    {
        documents = [];
        if (string.IsNullOrWhiteSpace(task.SdkCompilerDirectory))
        {
            return null;
        }

        var compilerPath = Path.Combine(task.SdkCompilerDirectory, "Microsoft.CodeAnalysis.dll");
        if (!File.Exists(compilerPath) || !IsNewer(AssemblyName.GetAssemblyName(compilerPath).Version!, typeof(Compilation).Assembly.GetName().Version!))
        {
            return null;
        }

        var taskAssembly = typeof(GenerateEmbeddedScreenplayDocuments).Assembly;
        var context = new SdkCompilerLoadContext(taskAssembly, task.SdkCompilerDirectory);
        try
        {
            var isolatedAssembly = context.LoadFromAssemblyPath(taskAssembly.Location);
            var taskType = isolatedAssembly.GetType(typeof(GenerateEmbeddedScreenplayDocuments).FullName!, throwOnError: true)!;
            var isolatedTask = (ITask)Activator.CreateInstance(taskType)!;
            isolatedTask.BuildEngine = task.BuildEngine;
            isolatedTask.HostObject = task.HostObject;

            foreach (var property in typeof(GenerateEmbeddedScreenplayDocuments).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(_ => _.SetMethod?.IsPublic == true))
            {
                taskType.GetProperty(property.Name)!.SetValue(isolatedTask, property.GetValue(task));
            }

            var result = isolatedTask.Execute();
            documents = (ITaskItem[])taskType.GetProperty(nameof(GenerateEmbeddedScreenplayDocuments.Documents))!.GetValue(isolatedTask)!;

            return result;
        }
        finally
        {
            context.Unload();
        }
    }
}
