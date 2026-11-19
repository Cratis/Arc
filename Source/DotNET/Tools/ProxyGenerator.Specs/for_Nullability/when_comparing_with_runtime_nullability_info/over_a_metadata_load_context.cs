// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
#pragma warning disable CS8618, CA1819 // CA1819: the fixture covers enum arrays, which made NullabilityInfoContext throw over a MetadataLoadContext on .NET 11.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Cratis.Arc.ProxyGenerator.for_Nullability.when_comparing_with_runtime_nullability_info;

public class over_a_metadata_load_context : Specification
{
    MetadataLoadContext _context;
    List<string> _mismatches = [];
    int _compared;

    void Establish()
    {
        var runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var paths = Directory.GetFiles(runtimeDirectory, "*.dll").Append(typeof(over_a_metadata_load_context).Assembly.Location);
        _context = new MetadataLoadContext(new PathAssemblyResolver(paths), typeof(object).Assembly.GetName().Name);
    }

    void Because()
    {
        var loadedType = _context.LoadFromAssemblyPath(typeof(over_a_metadata_load_context).Assembly.Location).GetType(typeof(Fixture).FullName!)!;
        var runtimeType = typeof(Fixture);
        var nullability = new NullabilityInfoContext();

        foreach (var runtimeProperty in runtimeType.GetProperties().Where(_ => !_.PropertyType.IsValueType))
        {
            var info = nullability.Create(runtimeProperty);
            var expected = (runtimeProperty.SetMethod is null ? info.ReadState : info.WriteState) == NullabilityState.Nullable;
            var actual = Nullability.IsNullable(loadedType.GetProperty(runtimeProperty.Name)!);
            _compared++;
            if (expected != actual)
            {
                _mismatches.Add($"property {runtimeProperty.Name}: expected {expected}, got {actual} [{string.Join(',', loadedType.GetProperty(runtimeProperty.Name)!.GetCustomAttributesData().Select(_ => _.AttributeType.FullName))}] {runtimeProperty.SetMethod is null}");
            }
        }

        foreach (var runtimeParameter in runtimeType.GetMethod(nameof(Fixture.Method))!.GetParameters().Where(_ => !_.ParameterType.IsValueType))
        {
            var expected = nullability.Create(runtimeParameter).WriteState == NullabilityState.Nullable;
            var actual = Nullability.IsNullable(loadedType.GetMethod(nameof(Fixture.Method))!.GetParameters().Single(_ => _.Name == runtimeParameter.Name));
            _compared++;
            if (expected != actual)
            {
                _mismatches.Add($"parameter {runtimeParameter.Name}: expected {expected}, got {actual}");
            }
        }
    }

    void Destroy() => _context.Dispose();

    [Fact] void should_compare_all_members() => _compared.ShouldEqual(18);
    [Fact] void should_match_runtime_nullability_info() => _mismatches.ShouldBeEmpty();

    public class Fixture
    {
        public string NotNull { get; set; }
        public string? Nullable { get; set; }
        public List<string>? NullableList { get; set; }
        public List<string?> ListOfNullable { get; set; }
        public IReadOnlyList<string?> NullableElements { get; set; }
        [AllowNull] public string Allowed { get; set; }
        [DisallowNull] public string? Disallowed { get; set; }
        [MaybeNull] public string MaybeReadOnly { get; }
        [NotNull] public string? NotNullReadOnly { get; }
        public Dictionary<string, string?>? NullableDictionary { get; set; }
        public DayOfWeek[] EnumArray { get; set; }
        public DayOfWeek[]? NullableEnumArray { get; set; }
        public IReadOnlyList<DayOfWeek> EnumList { get; set; }
        public DayOfWeek?[] ArrayOfNullableEnum { get; set; }

        public void Method(string required, string? optional, List<string>? optionalList, [AllowNull] string allowed)
        {
        }
    }
}
