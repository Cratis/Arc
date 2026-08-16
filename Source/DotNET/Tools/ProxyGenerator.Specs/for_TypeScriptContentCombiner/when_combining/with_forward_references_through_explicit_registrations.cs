// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.for_TypeScriptContentCombiner.when_combining;

public class with_forward_references_through_explicit_registrations : Specification, IDisposable
{
    const string ParentTypeName = "Drawing";
    const string BaseTypeName = "Shape";
    const string FirstDerivativeTypeName = "Circle";
    const string SecondDerivativeTypeName = "Rectangle";

#pragma warning disable MA0136 // Raw String contains an implicit end of line character
    const string ParentContent = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        // eslint-disable-next-line header/header
        import { field } from '@cratis/fundamentals';
        import { Shape } from './Shape';
        import { Circle } from './Circle';
        import { Rectangle } from './Rectangle';

        export class Drawing {
            selectedShape!: Shape;
            shapes!: Shape[];
        }
        field(Shape, false, [Circle, Rectangle])(Drawing.prototype, 'selectedShape');
        field(Shape, true, [Circle, Rectangle])(Drawing.prototype, 'shapes');
        """;

    const string CircleContent = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        // eslint-disable-next-line header/header
        import { field, derivedType } from '@cratis/fundamentals';
        import { Shape } from './Shape';

        export class Circle extends Shape {
            radius!: number;
        }
        field(Number)(Circle.prototype, 'radius');
        derivedType('circle')(Circle);
        """;

    const string RectangleContent = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        // eslint-disable-next-line header/header
        import { field, derivedType } from '@cratis/fundamentals';
        import { Shape } from './Shape';

        export class Rectangle extends Shape {
            width!: number;
        }
        field(Number)(Rectangle.prototype, 'width');
        derivedType('rectangle')(Rectangle);
        """;

    const string ShapeContent = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        // eslint-disable-next-line header/header
        import { field } from '@cratis/fundamentals';

        export class Shape {
            name!: string;
        }
        field(String)(Shape.prototype, 'name');
        """;
#pragma warning restore MA0136 // Raw String contains an implicit end of line character

    JavaScriptRuntime _runtime = null!;
    string _result = null!;
    IReadOnlyList<string> _diagnostics = null!;

    void Establish() => _runtime = new JavaScriptRuntime();

    void Because()
    {
        _result = TypeScriptContentCombiner.Combine([ParentContent, CircleContent, RectangleContent, ShapeContent]);
        _diagnostics = _runtime.GetSyntacticDiagnostics(_result, experimentalDecorators: false);
    }

    [Fact] void should_declare_the_base_before_the_parent() => IndexOfType(BaseTypeName).ShouldBeLessThan(IndexOfType(ParentTypeName));
    [Fact] void should_declare_the_first_derivative_before_the_parent() => IndexOfType(FirstDerivativeTypeName).ShouldBeLessThan(IndexOfType(ParentTypeName));
    [Fact] void should_declare_the_second_derivative_before_the_parent() => IndexOfType(SecondDerivativeTypeName).ShouldBeLessThan(IndexOfType(ParentTypeName));
    [Fact] void should_keep_the_derivative_constructor_array() => _result.ShouldContain("field(Shape, true, [Circle, Rectangle])(Drawing.prototype, 'shapes');");
    [Fact] void should_merge_the_fundamentals_imports() => _result.ShouldContain("import { field, derivedType } from '@cratis/fundamentals';");
    [Fact] void should_only_emit_one_fundamentals_import() => _result.Split("from '@cratis/fundamentals';").Length.ShouldEqual(2);
    [Fact] void should_remove_the_internal_base_import() => _result.ShouldNotContain("import { Shape } from './Shape';");
    [Fact] void should_remove_the_internal_derivative_imports() => _result.ShouldNotContain("import { Circle } from './Circle';");
    [Fact] void should_keep_registration_after_its_class_declaration() => _result.IndexOf("derivedType('circle')(Circle);", StringComparison.Ordinal).ShouldBeGreaterThan(IndexOfType(FirstDerivativeTypeName));
    [Fact] void should_not_contain_decorator_syntax() => _result.ShouldNotContain("@field(");
    [Fact] void should_compile_with_experimental_decorators_disabled() => _diagnostics.ShouldBeEmpty();

    public void Dispose()
    {
        _runtime?.Dispose();
        GC.SuppressFinalize(this);
    }

    int IndexOfType(string typeName) => _result.IndexOf($"export class {typeName}", StringComparison.Ordinal);
}
