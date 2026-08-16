// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_TemplateTypes.when_generating_type;

public class with_explicit_metadata_registration : Specification, IDisposable
{
    JavaScriptRuntime _runtime = null!;
    TypeDescriptor _descriptor = null!;
    string _result = null!;
    string _transpiledResult = null!;
    IReadOnlyList<string> _diagnostics = null!;

    void Establish()
    {
        _runtime = new JavaScriptRuntime();

        PropertyDescriptor[] properties =
        [
            new(typeof(string), "Name", "string", "String", string.Empty, false, false, true, null),
            new(typeof(int[]), "Scores", "number", "Number", string.Empty, true, false, true, null),
            new(typeof(ExplicitRegistrationStatus), "Status", "ExplicitRegistrationStatus", "Number", string.Empty, false, false, false, null),
            new(typeof(DateTime), "OccurredAt", "Date", "Date", string.Empty, false, false, true, null),
            new(typeof(ExplicitRegistrationNestedType), "Nested", "ExplicitRegistrationNestedType", "ExplicitRegistrationNestedType", string.Empty, false, false, false, null),
            new(typeof(ExplicitRegistrationNestedType[]), "NestedItems", "ExplicitRegistrationNestedType", "ExplicitRegistrationNestedType", string.Empty, true, false, false, null),
            new(typeof(IExplicitRegistrationShape), "Shape", "IExplicitRegistrationShape", "IExplicitRegistrationShape", string.Empty, false, false, false, null, "ExplicitRegistrationCircle, ExplicitRegistrationRectangle"),
            new(typeof(IExplicitRegistrationShape[]), "Shapes", "IExplicitRegistrationShape", "IExplicitRegistrationShape", string.Empty, true, false, false, null, "ExplicitRegistrationCircle, ExplicitRegistrationRectangle")
        ];

        ImportStatement[] imports =
        [
            new(typeof(ExplicitRegistrationStatus), nameof(ExplicitRegistrationStatus), $"./{nameof(ExplicitRegistrationStatus)}"),
            new(typeof(ExplicitRegistrationNestedType), nameof(ExplicitRegistrationNestedType), $"./{nameof(ExplicitRegistrationNestedType)}"),
            new(typeof(IExplicitRegistrationShape), nameof(IExplicitRegistrationShape), $"./{nameof(IExplicitRegistrationShape)}"),
            new(typeof(ExplicitRegistrationCircle), nameof(ExplicitRegistrationCircle), $"./{nameof(ExplicitRegistrationCircle)}"),
            new(typeof(ExplicitRegistrationRectangle), nameof(ExplicitRegistrationRectangle), $"./{nameof(ExplicitRegistrationRectangle)}")
        ];

        _descriptor = new TypeDescriptor(
            typeof(TypeWithExplicitMetadataRegistration),
            "TypeWithExplicitMetadataRegistration",
            properties,
            imports.OrderBy(_ => _.Module),
            [],
            UseExplicitMetadataRegistration: true);
    }

    void Because()
    {
        _result = TemplateTypes.Type(_descriptor);
        _transpiledResult = _runtime.TranspileTypeScript(_result, experimentalDecorators: false);
        _diagnostics = _runtime.GetSyntacticDiagnostics(_result, experimentalDecorators: false);
    }

    [Fact] void should_import_the_field_factory() => _result.ShouldContain("import { field } from '@cratis/fundamentals';");
    [Fact] void should_not_emit_field_decorator_syntax() => _result.ShouldNotContain("@field(");
    [Fact] void should_not_emit_derived_type_decorator_syntax() => _result.ShouldNotContain("@derivedType(");
    [Fact] void should_declare_the_primitive_property() => _result.ShouldContain("name!: string;");
    [Fact] void should_register_the_primitive_field() => _result.ShouldContain("field(String)(TypeWithExplicitMetadataRegistration.prototype, 'name');");
    [Fact] void should_declare_the_collection_as_an_array() => _result.ShouldContain("scores!: number[];");
    [Fact] void should_register_the_collection_as_enumerable() => _result.ShouldContain("field(Number, true)(TypeWithExplicitMetadataRegistration.prototype, 'scores');");
    [Fact] void should_declare_the_enum_property() => _result.ShouldContain("status!: ExplicitRegistrationStatus;");
    [Fact] void should_register_the_enum_with_its_numeric_constructor() => _result.ShouldContain("field(Number)(TypeWithExplicitMetadataRegistration.prototype, 'status');");
    [Fact] void should_declare_the_date_property() => _result.ShouldContain("occurredAt!: Date;");
    [Fact] void should_register_the_date_field() => _result.ShouldContain("field(Date)(TypeWithExplicitMetadataRegistration.prototype, 'occurredAt');");
    [Fact] void should_declare_the_typed_property() => _result.ShouldContain("nested!: ExplicitRegistrationNestedType;");
    [Fact] void should_register_the_typed_field() => _result.ShouldContain("field(ExplicitRegistrationNestedType)(TypeWithExplicitMetadataRegistration.prototype, 'nested');");
    [Fact] void should_declare_the_typed_collection_as_an_array() => _result.ShouldContain("nestedItems!: ExplicitRegistrationNestedType[];");
    [Fact] void should_register_the_typed_collection_as_enumerable() => _result.ShouldContain("field(ExplicitRegistrationNestedType, true)(TypeWithExplicitMetadataRegistration.prototype, 'nestedItems');");
    [Fact] void should_register_the_interface_field_with_its_derivatives() => _result.ShouldContain("field(IExplicitRegistrationShape, false, [ExplicitRegistrationCircle, ExplicitRegistrationRectangle])(TypeWithExplicitMetadataRegistration.prototype, 'shape');");
    [Fact] void should_register_the_interface_collection_with_its_derivatives() => _result.ShouldContain("field(IExplicitRegistrationShape, true, [ExplicitRegistrationCircle, ExplicitRegistrationRectangle])(TypeWithExplicitMetadataRegistration.prototype, 'shapes');");
    [Fact] void should_import_the_typed_field() => _result.ShouldContain($"import {{ {nameof(ExplicitRegistrationNestedType)} }} from './{nameof(ExplicitRegistrationNestedType)}';");
    [Fact] void should_import_the_interface_derivatives() => _result.ShouldContain($"import {{ {nameof(ExplicitRegistrationCircle)} }} from './{nameof(ExplicitRegistrationCircle)}';");
    [Fact] void should_transpile_with_experimental_decorators_disabled() => _transpiledResult.ShouldNotBeEmpty();
    [Fact] void should_produce_no_typescript_diagnostics_with_experimental_decorators_disabled() => _diagnostics.ShouldBeEmpty();

    public void Dispose()
    {
        _runtime?.Dispose();
        GC.SuppressFinalize(this);
    }
}

public class TypeWithExplicitMetadataRegistration;

public class ExplicitRegistrationNestedType;

public interface IExplicitRegistrationShape;

public class ExplicitRegistrationCircle : IExplicitRegistrationShape;

public class ExplicitRegistrationRectangle : IExplicitRegistrationShape;

public enum ExplicitRegistrationStatus
{
    Unknown = 0,
    Active = 1
}
