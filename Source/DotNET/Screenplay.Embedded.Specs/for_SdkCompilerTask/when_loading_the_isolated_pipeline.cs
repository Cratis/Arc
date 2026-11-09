// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.Loader;
using Cratis.Arc.Screenplay.Embedded.Build;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Embedded.for_SdkCompilerTask;

public class when_loading_the_isolated_pipeline : Specification
{
    SdkCompilerLoadContext _context;
    Assembly _compiler;
    Type _task;

    void Establish() => _context = new(typeof(GenerateEmbeddedScreenplayDocuments).Assembly, Path.GetDirectoryName(typeof(Compilation).Assembly.Location)!);

    void Because()
    {
        _compiler = _context.LoadFromAssemblyName(typeof(Compilation).Assembly.GetName());
        _task = _context.LoadFromAssemblyPath(typeof(GenerateEmbeddedScreenplayDocuments).Assembly.Location).GetType(typeof(GenerateEmbeddedScreenplayDocuments).FullName!)!;
    }

    [Fact] void should_isolate_the_compiler() => AssemblyLoadContext.GetLoadContext(_compiler).ShouldEqual(_context);
    [Fact] void should_isolate_the_task() => AssemblyLoadContext.GetLoadContext(_task.Assembly).ShouldEqual(_context);
    [Fact] void should_share_the_msbuild_task_contract() => typeof(ITask).IsAssignableFrom(_task).ShouldBeTrue();
    [Fact] void should_share_the_resource_item_contract() => _task.GetProperty(nameof(GenerateEmbeddedScreenplayDocuments.Documents))!.PropertyType.ShouldEqual(typeof(ITaskItem[]));

    void Destroy() => _context.Unload();
}
