// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Arc.MongoDB.for_DerivedTypeDiscriminatorConvention;

#pragma warning disable SA1402, SA1649 // Fixture types for the polymorphic read-model specs live together deliberately.

/// <summary>
/// Fixture hierarchy mirroring the shape that crashed in production: a read model embedding a polymorphic
/// element tree (marker interface → abstract base chain → [DerivedType] concrete leaves) and an
/// interface-typed rules list, both discriminated by "_derivedTypeId".
/// </summary>
public interface IVisual;

public abstract class VisualObject
{
    public string Id { get; set; } = string.Empty;

    public IDictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();
}

public abstract class Visual : VisualObject, IVisual
{
    public bool IsEnabled { get; set; } = true;

    public double Opacity { get; set; } = 1;

    public double? Width { get; set; }

    public double? Height { get; set; }
}

public abstract class FrameworkVisual : Visual
{
    public string? Name { get; set; }

    public double MinWidth { get; set; }

    public double MaxWidth { get; set; } = double.MaxValue;
}

[DerivedType("Test.Button", typeof(IVisual))]
public class ButtonVisual : FrameworkVisual
{
    public bool Rounded { get; set; }

    public int Severity { get; set; }
}

[DerivedType("Test.Knob", typeof(IVisual))]
public class KnobVisual : FrameworkVisual
{
    public int Size { get; set; }
}

public interface IPropertyRule
{
    string? ErrorMessage { get; }
}

[DerivedType("notEmpty", typeof(IPropertyRule))]
public record NotEmptyRule(string? ErrorMessage = null) : IPropertyRule;

public record RulesForProperty(string PropertyName, IReadOnlyList<IPropertyRule> Rules);

public record CommandElement(string Id, string Name, IReadOnlyList<RulesForProperty>? Rules = null);

public record ActorElement(string Id, IReadOnlyList<Visual> Elements);

public record SliceElement(string Id, string Name, IReadOnlyList<ActorElement> Actors, CommandElement? Command = null);

public record FeatureElement(string Id, string Name, IReadOnlyList<SliceElement> Slices);

public record ModuleReadModel(string Id, string Name, IReadOnlyList<FeatureElement> Features);

#pragma warning restore SA1402, SA1649
