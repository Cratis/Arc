// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

public class when_generating_flags_enum_type : Specification, IDisposable
{
    JavaScriptRuntime _runtime = null!;
    string _generatedCode = null!;
    EnumDescriptor _descriptor = null!;
    bool _typeScriptIsValid;

    void Establish()
    {
        _runtime = new JavaScriptRuntime();

        var enumType = typeof(AnchorEdges);
        var members = Enum.GetValues(enumType)
            .Cast<AnchorEdges>()
            .Select(v => new EnumMemberDescriptor(v.ToString(), (int)v))
            .ToList();

        _descriptor = new EnumDescriptor(
            enumType,
            nameof(AnchorEdges),
            members,
            [],
            IsFlags: true);
    }

    void Because()
    {
        _generatedCode = InMemoryProxyGenerator.GenerateFlagsEnum(_descriptor);

        try
        {
            var transpiledCode = _runtime.TranspileTypeScript(_generatedCode);
            _typeScriptIsValid = !string.IsNullOrEmpty(transpiledCode);
        }
        catch
        {
            _typeScriptIsValid = false;
        }
    }

    [Fact] void should_generate_code() => _generatedCode.ShouldNotBeEmpty();
    [Fact] void should_contain_enum_name() => _generatedCode.ShouldContain("AnchorEdges");
    [Fact] void should_contain_none_member() => _generatedCode.ShouldContain("none");
    [Fact] void should_contain_top_member() => _generatedCode.ShouldContain("top");
    [Fact] void should_contain_right_member() => _generatedCode.ShouldContain("right");
    [Fact] void should_contain_bottom_member() => _generatedCode.ShouldContain("bottom");
    [Fact] void should_contain_left_member() => _generatedCode.ShouldContain("left");
    [Fact] void should_contain_all_constant() => _generatedCode.ShouldContain("allAnchorEdges");
    [Fact] void should_combine_top_in_all_constant() => _generatedCode.ShouldContain("AnchorEdges.top");
    [Fact] void should_combine_right_in_all_constant() => _generatedCode.ShouldContain("AnchorEdges.right");
    [Fact] void should_combine_bottom_in_all_constant() => _generatedCode.ShouldContain("AnchorEdges.bottom");
    [Fact] void should_combine_left_in_all_constant() => _generatedCode.ShouldContain("AnchorEdges.left");
    [Fact] void should_not_include_none_in_all_constant() => _generatedCode.ShouldNotContain("AnchorEdges.none");
    [Fact] void should_use_bitwise_or_operator() => _generatedCode.ShouldContain("|");
    [Fact] void should_be_valid_typescript() => _typeScriptIsValid.ShouldBeTrue();

    public void Dispose()
    {
        _runtime?.Dispose();
        GC.SuppressFinalize(this);
    }
}
