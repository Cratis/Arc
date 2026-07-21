// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Templates;

/// <summary>
/// A regular-expression pattern carried as a rule argument, so the formatter emits it as a JavaScript regular
/// expression literal rather than a string - the client-side <c>matches</c> rule takes a <see cref="System.Text.RegularExpressions.Regex"/>, not a string.
/// </summary>
/// <param name="Pattern">The regular expression pattern.</param>
public record RegularExpressionPattern(string Pattern);
