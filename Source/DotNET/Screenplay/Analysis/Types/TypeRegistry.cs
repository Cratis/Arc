// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Types;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Types;

/// <summary>
/// Resolves the type of a property, a parameter or a return value, collecting the concepts encountered along the way.
/// </summary>
/// <remarks>
/// Resolving a type is answering two questions at once. The first is what to write - a single identifier, whether
/// there is one of it or many, and whether it may be absent. The second is what naming it commits the document to,
/// because every concept reached on the way has to be declared before it can be referenced, and every name that says
/// less than the type does has to be reported rather than passed off as a description.
/// <para>
/// The first question is answered here. The second is split: what a name loses and which shapes no declaration can
/// hold are kept here because they are consequences of writing the name, while the concepts themselves are kept by a
/// <see cref="ConceptRegistry"/>, which decides what a concept is and what happens when two of them share a name.
/// </para>
/// </remarks>
public class TypeRegistry
{
    readonly ConceptRegistry _concepts = new();
    readonly HashSet<string> _unmappable = new(StringComparer.Ordinal);
    readonly HashSet<string> _shapes = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the full name of every type that had to be referred to by a name that does not say what it is.
    /// </summary>
    public IEnumerable<string> Unmappable => _unmappable.Order(StringComparer.Ordinal);

    /// <summary>
    /// Gets the full name of every record a property carries whose shape no declaration can hold.
    /// </summary>
    public IEnumerable<string> Shapes => _shapes.Order(StringComparer.Ordinal);

    /// <summary>
    /// Gets the full name of every type whose simple name a concept was already declared under.
    /// </summary>
    public IEnumerable<string> Ambiguous => _concepts.Ambiguous;

    /// <summary>
    /// Gets every concept referenced by the application, ordered by name.
    /// </summary>
    public IEnumerable<ConceptModel> Concepts => _concepts.Concepts;

    /// <summary>
    /// Resolves the Screenplay type reference a symbol corresponds to.
    /// </summary>
    /// <param name="type">The type to resolve.</param>
    /// <returns>The <see cref="TypeReferenceModel"/>.</returns>
    public TypeReferenceModel Resolve(ITypeSymbol type)
    {
        var optional = false;
        var collection = false;

        return new(NameOf(UnderlyingTypes.Of(type, ref optional, ref collection)), collection, optional);
    }

    /// <summary>
    /// Resolves the Screenplay type reference of a value an artifact carries.
    /// </summary>
    /// <param name="type">The type to resolve.</param>
    /// <returns>The <see cref="TypeReferenceModel"/>.</returns>
    /// <remarks>
    /// A property is where a record the document has no way to declare is really referred to - the line carrying it
    /// names a shape nothing in the document introduces. That is asked here rather than everywhere a type is resolved,
    /// because a query returning a read model refers to something the slice around it already describes, while a
    /// property carrying a record refers to a shape stated nowhere at all.
    /// </remarks>
    public TypeReferenceModel ResolveCarried(ITypeSymbol type)
    {
        var optional = false;
        var collection = false;
        var carried = UnderlyingTypes.Of(type, ref optional, ref collection);

        if (CarriedTypes.IsRecord(carried))
        {
            _shapes.Add(carried.ToDisplayString());
        }

        return new(NameOf(carried), collection, optional);
    }

    /// <summary>
    /// Records that a value of a concept carries personally identifiable information.
    /// </summary>
    /// <param name="type">The type of the value.</param>
    public void MarkAsPii(ITypeSymbol type) => _concepts.MarkAsPii(type);

    /// <summary>
    /// Records the validation rules a concept declares for itself.
    /// </summary>
    /// <param name="conceptName">The name of the concept.</param>
    /// <param name="rules">The rules to record.</param>
    public void AddValidations(string conceptName, IEnumerable<ValidationRuleModel> rules) =>
        _concepts.AddValidations(conceptName, rules);

    /// <summary>
    /// Resolves the name a type is referenced by, registering it as a concept when it is one.
    /// </summary>
    /// <param name="type">The type to name.</param>
    /// <returns>The Screenplay type name.</returns>
    string NameOf(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named && ScreenplayPrimitiveTypes.TryResolve(named.FullMetadataName(), out var primitive))
        {
            return ScreenplayPrimitiveTypes.GetName(primitive);
        }

        if (_concepts.TryRegister(type))
        {
            return type.Name;
        }

        RegisterWhatItCarries(type);
        ReportWhatTheNameLoses(type);

        return type.Name;
    }

    /// <summary>
    /// Registers every concept a record carries, however far down it is carried.
    /// </summary>
    /// <param name="type">The type being named.</param>
    /// <remarks>
    /// A record is referred to by name and never declared, so nothing inside it is ever named on its own - which left
    /// every concept reached only through a line of a timesheet or a property of a read model out of the document
    /// entirely. A concept can be declared wherever it was reached from, so it is, and the shape carrying it waits on
    /// the language.
    /// </remarks>
    void RegisterWhatItCarries(ITypeSymbol type)
    {
        foreach (var carried in CarriedTypes.Within(type))
        {
            _concepts.TryRegister(carried);
        }
    }

    /// <summary>
    /// Records a type whose simple name says less than the type does.
    /// </summary>
    /// <param name="type">The type being named.</param>
    /// <remarks>
    /// A read model or a nested object referred to by its own name is exactly right. A constructed generic is not -
    /// writing <c>IDictionary&lt;string, string&gt;</c> as the single identifier the grammar allows leaves the word
    /// <c>KeyValuePair</c> behind, which says nothing and which the document never declares. Same for a type
    /// parameter, whose name is a placeholder rather than a type.
    /// </remarks>
    void ReportWhatTheNameLoses(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { TypeArguments.Length: > 0 } or { TypeKind: TypeKind.TypeParameter })
        {
            _unmappable.Add(type.ToDisplayString());
        }
    }
}
