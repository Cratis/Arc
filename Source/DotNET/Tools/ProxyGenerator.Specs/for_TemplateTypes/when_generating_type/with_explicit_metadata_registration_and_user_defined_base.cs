// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_TemplateTypes.when_generating_type;

public class with_explicit_metadata_registration_and_user_defined_base : Specification, IDisposable
{
    const string DerivedIdentifier = "explicit-derived-type";

    JavaScriptRuntime _runtime = null!;
    TypeDescriptor _baseDescriptor = null!;
    TypeDescriptor _derivedDescriptor = null!;
    string _derivedResult = null!;
    string _registeredDerivedIdentifier = null!;
    string _registeredFieldNames = null!;
    bool _derivedTypeWasRegisteredForBase;
    bool _deserializedInstanceIsDerived;
    string _deserializedValues = null!;

    void Establish()
    {
        _runtime = new JavaScriptRuntime();

        PropertyDescriptor[] baseProperties =
        [
            new(typeof(string), "BaseValue", "string", "String", string.Empty, false, false, true, null)
        ];
        _baseDescriptor = new TypeDescriptor(
            typeof(ExplicitMetadataBaseType),
            nameof(ExplicitMetadataBaseType),
            baseProperties,
            Enumerable.Empty<ImportStatement>().OrderBy(_ => _.Module),
            [],
            UseExplicitMetadataRegistration: true);

        PropertyDescriptor[] derivedProperties =
        [
            new(typeof(int), "DerivedValue", "number", "Number", string.Empty, false, false, true, null)
        ];
        ImportStatement[] imports =
        [
            new(typeof(ExplicitMetadataBaseType), nameof(ExplicitMetadataBaseType), $"./{nameof(ExplicitMetadataBaseType)}")
        ];
        _derivedDescriptor = new TypeDescriptor(
            typeof(ExplicitMetadataDerivedType),
            nameof(ExplicitMetadataDerivedType),
            derivedProperties,
            imports.OrderBy(_ => _.Module),
            [],
            DerivedTypeId: DerivedIdentifier,
            BaseTypeName: nameof(ExplicitMetadataBaseType),
            UseExplicitMetadataRegistration: true);
    }

    void Because()
    {
        var baseResult = TemplateTypes.Type(_baseDescriptor);
        _derivedResult = TemplateTypes.Type(_derivedDescriptor);

        ExecuteTypeScriptModule(baseResult, nameof(ExplicitMetadataBaseType));
        ExecuteTypeScriptModule(_derivedResult, nameof(ExplicitMetadataDerivedType));

        _registeredDerivedIdentifier = _runtime.Evaluate<string>(
            $"require('@cratis/fundamentals').DerivedType.get(globalThis.{nameof(ExplicitMetadataDerivedType)})")!;
        _derivedTypeWasRegisteredForBase = _runtime.Evaluate<bool>(
            $"require('@cratis/fundamentals').DerivedType.getDerivedTypesFor(globalThis.{nameof(ExplicitMetadataBaseType)}).includes(globalThis.{nameof(ExplicitMetadataDerivedType)})");
        _registeredFieldNames = _runtime.Evaluate<string>(
            $"require('@cratis/fundamentals').Fields.getFieldsForType(globalThis.{nameof(ExplicitMetadataDerivedType)}).map(field => field.name).join(',')")!;
        _runtime.Execute(
            $"globalThis.__explicitMetadataResult = require('@cratis/fundamentals').JsonSerializer.deserializeFromInstance(globalThis.{nameof(ExplicitMetadataDerivedType)}, {{ baseValue: 'base', derivedValue: 42 }});");
        _deserializedInstanceIsDerived = _runtime.Evaluate<bool>(
            $"globalThis.__explicitMetadataResult instanceof globalThis.{nameof(ExplicitMetadataDerivedType)}");
        _deserializedValues = _runtime.Evaluate<string>(
            "`${globalThis.__explicitMetadataResult.baseValue}:${globalThis.__explicitMetadataResult.derivedValue}`")!;
    }

    [Fact] void should_import_both_metadata_factories() => _derivedResult.ShouldContain("import { field, derivedType } from '@cratis/fundamentals';");
    [Fact] void should_extend_the_user_defined_base() => _derivedResult.ShouldContain($"export class {nameof(ExplicitMetadataDerivedType)} extends {nameof(ExplicitMetadataBaseType)}");
    [Fact] void should_register_the_derived_type_imperatively() => _derivedResult.ShouldContain($"derivedType('{DerivedIdentifier}')({nameof(ExplicitMetadataDerivedType)});");
    [Fact] void should_not_emit_derived_type_decorator_syntax() => _derivedResult.ShouldNotContain("@derivedType(");
    [Fact] void should_preserve_the_derived_type_identifier_at_runtime() => _registeredDerivedIdentifier.ShouldEqual(DerivedIdentifier);
    [Fact] void should_register_the_derived_type_with_its_base_at_runtime() => _derivedTypeWasRegisteredForBase.ShouldBeTrue();
    [Fact] void should_make_base_and_derived_fields_available_at_runtime() => _registeredFieldNames.ShouldEqual("baseValue,derivedValue");
    [Fact] void should_deserialize_to_the_derived_type() => _deserializedInstanceIsDerived.ShouldBeTrue();
    [Fact] void should_deserialize_base_and_derived_fields() => _deserializedValues.ShouldEqual("base:42");

    public void Dispose()
    {
        _runtime?.Dispose();
        GC.SuppressFinalize(this);
    }

    void ExecuteTypeScriptModule(string typeScript, string exportedTypeName)
    {
        var javaScript = _runtime.TranspileTypeScript(typeScript, experimentalDecorators: false);
#pragma warning disable MA0136 // Raw String contains an implicit end of line character
        _runtime.Execute(
            $$"""
            (() => {
                const module = { exports: {} };
                const exports = module.exports;
                const require = globalThis.require;
                {{javaScript}}
                globalThis.{{exportedTypeName}} = module.exports.{{exportedTypeName}};
            })();
            """);
#pragma warning restore MA0136 // Raw String contains an implicit end of line character
    }
}

public class ExplicitMetadataBaseType
{
    public string BaseValue { get; set; } = string.Empty;
}

public class ExplicitMetadataDerivedType : ExplicitMetadataBaseType
{
    public int DerivedValue { get; set; }
}
