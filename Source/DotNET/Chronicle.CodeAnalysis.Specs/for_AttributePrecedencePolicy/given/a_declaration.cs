// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_AttributePrecedencePolicy.given;

public class a_declaration : Specification
{
    const string Imports = "using Cratis.Arc.Commands; using Cratis.Arc.Chronicle.ReadModels;\n";
    protected string _action = "read_protection";
    protected string _source = string.Empty;
    protected string _foreignSource = string.Empty;
    protected string _commandName = "Command";
    protected string _modelName = "Model";
    protected string _methodName = "Handle";
    protected bool _hasParameter = true;
    protected bool _hasMethod = true;
    protected bool _isReadModel = true;
    protected bool _genericModel;
    protected string _runtime = string.Empty;
    protected string _roslyn = string.Empty;
    protected string _runtimeRead = string.Empty;
    protected string _roslynRead = string.Empty;

    protected void Resolve()
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Concat([typeof(ReadProtectionAttribute).Assembly.Location, typeof(UnprotectedAttribute).Assembly.Location])
            .Distinct(StringComparer.Ordinal).Select(path => MetadataReference.CreateFromFile(path)).ToList();
        CSharpCompilation? foreign = null;
        Assembly? foreignAssembly = null;
        if (_foreignSource.Length > 0)
        {
            foreign = Compile(_foreignSource, references);
            foreignAssembly = Emit(foreign);
            references.Add(MetadataReference.CreateFromImage(Image(foreign)));
        }

        var compilation = Compile(_source, references);
        var assembly = Emit(compilation);
        var command = assembly.GetType(_commandName)!;
        var model = foreignAssembly?.GetType(_modelName) ?? assembly.GetType(_genericModel ? "Model`1" : _modelName)!;
        var method = command.GetMethod(_methodName);
        var parameter = _hasParameter ? method?.GetParameters().FirstOrDefault() : null;
        if (_genericModel)
        {
            model = method!.GetParameters()[0].ParameterType;
        }

        var commandSymbol = compilation.GetTypeByMetadataName(_commandName)!;
        var modelSymbol = compilation.GetTypeByMetadataName(_genericModel ? "Model`1" : _modelName)!;
        var methodSymbol = commandSymbol.GetMembers(_methodName).OfType<IMethodSymbol>().FirstOrDefault();
        var parameterSymbol = _hasParameter ? methodSymbol?.Parameters.FirstOrDefault() : null;
        if (_genericModel)
        {
            modelSymbol = (INamedTypeSymbol)methodSymbol!.Parameters[0].Type;
        }

        if (_action == "read_protection")
        {
            var runtime = CommandAttributePolicy.ResolveCommand(command);
            var roslyn = CommandAttributeSymbols.ResolveCommand(commandSymbol);
            var error = Record.Exception(() => CommandAttributePolicy.ThrowIfConflicting(command, runtime.Conflict));
            _runtime = $"{runtime.Mode}:{runtime.ReadProtection}:{error?.GetType().Name}";
            _roslyn = $"{roslyn.Mode}:{roslyn.ReadProtection}:{roslyn.Conflict}";
        }
        else if (_action == "read_policy")
        {
            var runtime = CommandAttributePolicy.ResolveRead(model, _hasMethod ? method : null, parameter);
            var roslyn = CommandAttributeSymbols.ResolveRead(modelSymbol, _hasMethod ? methodSymbol : null, parameterSymbol);
            _runtime = $"{runtime.Advisory}:{runtime.MethodBodyUnprotected}";
            _roslyn = $"{roslyn.Advisory}:{roslyn.MethodBodyUnprotected}";
        }
        else
        {
            var lookup = CommandAttributePolicy.ResolveLookup(command, parameter!, _isReadModel);
            var error = Record.Exception(() => CommandAttributePolicy.ThrowIfInvalidLookup(command, "declared", lookup.Conflict));
            _runtime = error?.GetType().Name ?? lookup.Property ?? string.Empty;
            var symbolLookup = CommandAttributeSymbols.ResolveLookup(commandSymbol, parameterSymbol!, _isReadModel);
            _roslyn = symbolLookup.Conflict ?? symbolLookup.Property ?? string.Empty;
            var runtimeRead = CommandAttributePolicy.ResolveRead(model, method, parameter);
            var roslynRead = CommandAttributeSymbols.ResolveRead(modelSymbol, methodSymbol, parameterSymbol);
            _runtimeRead = $"{runtimeRead.Advisory}:{runtimeRead.MethodBodyUnprotected}";
            _roslynRead = $"{roslynRead.Advisory}:{roslynRead.MethodBodyUnprotected}";
        }
    }

    static CSharpCompilation Compile(string source, IEnumerable<MetadataReference> references) => CSharpCompilation.Create(
        $"Declarations_{Guid.NewGuid():N}",
        [CSharpSyntaxTree.ParseText(Imports + source)],
        references,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    static byte[] Image(CSharpCompilation compilation)
    {
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        return stream.ToArray();
    }

    static Assembly Emit(CSharpCompilation compilation) => Assembly.Load(Image(compilation));
}
