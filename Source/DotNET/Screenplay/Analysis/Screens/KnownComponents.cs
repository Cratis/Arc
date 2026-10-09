// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Represents the Cratis Components a user interface file imports, under the names it uses them by.
/// </summary>
/// <param name="Imports">Everything the file imports, by the name it uses.</param>
/// <remarks>
/// A component is recognized by where it comes from as well as by its name. A <c>DataPage</c> imported from anywhere
/// but <c>@cratis/components</c> is someone else's component with the same name, and its properties mean whatever its
/// author decided.
/// </remarks>
public record KnownComponents(IReadOnlyDictionary<string, ScreenImportBinding> Imports)
{
    /// <summary>
    /// The package Cratis Components are published as.
    /// </summary>
    public const string Package = "@cratis/components";

    /// <summary>
    /// Gets the names the file uses a data page by.
    /// </summary>
    public IEnumerable<string> DataPages => Named("DataPage");

    /// <summary>
    /// Gets the names the file uses a data table bound to a query by.
    /// </summary>
    public IEnumerable<string> DataTables => Named("DataTableForQuery").Concat(Named("DataTableForObservableQuery"));

    /// <summary>
    /// Gets the names the file uses a command dialog by.
    /// </summary>
    public IEnumerable<string> CommandDialogs => Named("CommandDialog");

    /// <summary>
    /// Gets the names the file uses anything exported as <c>Column</c> by, wherever it comes from.
    /// </summary>
    /// <remarks>
    /// Columns are found whoever wrote them, so a column that is not a Cratis Components one can be reported rather
    /// than passed over in silence.
    /// </remarks>
    public IEnumerable<ScreenImportBinding> Columns => Imports.Values.Where(_ => _.Name == "Column").OrderBy(_ => _.Local, StringComparer.Ordinal);

    /// <summary>
    /// Gets whether a binding is one of the Cratis Components.
    /// </summary>
    /// <param name="binding">The binding.</param>
    /// <returns>True when it is imported from the Cratis Components package.</returns>
    public static bool IsCratis(ScreenImportBinding binding) =>
        binding.Module == Package || binding.Module.StartsWith($"{Package}/", StringComparison.Ordinal);

    /// <summary>
    /// Reads the components a file imports.
    /// </summary>
    /// <param name="text">The text of the file.</param>
    /// <returns>The <see cref="KnownComponents"/>.</returns>
    public static KnownComponents In(string text) => new(ScreenImports.Bindings(text));

    /// <summary>
    /// Gets what a name the file uses is bound to, when it is imported from a file alongside it.
    /// </summary>
    /// <param name="local">The name the file uses.</param>
    /// <returns>The name the module exports it under, or null when it is not imported from a file alongside it.</returns>
    public string? Proxy(string local) => Imports.TryGetValue(local, out var binding) && binding.IsRelative ? binding.Name : null;

    IEnumerable<string> Named(string name) =>
        Imports.Values.Where(_ => _.Name == name && IsCratis(_)).Select(_ => _.Local).Order(StringComparer.Ordinal);
}
