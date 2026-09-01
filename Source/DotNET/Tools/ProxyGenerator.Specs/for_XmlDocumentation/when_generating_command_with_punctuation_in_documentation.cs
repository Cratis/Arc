// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.ModelBound;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_XmlDocumentation;

/// <summary>
/// A generated proxy is TypeScript, and its documentation is a JSDoc comment rather than markup, so the text has to
/// arrive as it was written. Rendering it through an escaping template turns every apostrophe into "&amp;#x27;",
/// which is what a reader then sees in their editor's tooltip.
/// </summary>
public class when_generating_command_with_punctuation_in_documentation : Specification
{
    CommandDescriptor _descriptor = null!;
    string _generatedCode = null!;

    void Establish()
    {
        var commandType = typeof(Scenarios.for_ProxyGeneration.CommandWithPunctuatedDocumentation);
        _descriptor = commandType.GetTypeInfo().ToCommandDescriptor(
            "/output",
            segmentsToSkip: 3,
            skipCommandNameInRoute: false,
            apiPrefix: "api",
            [commandType.GetTypeInfo()]);
    }

    void Because() => _generatedCode = TemplateTypes.Command(_descriptor);

    [Fact] void should_keep_an_apostrophe_in_the_type_documentation() => _generatedCode.ShouldContain("Renames an issue's title");
    [Fact] void should_keep_an_apostrophe_in_the_property_documentation() => _generatedCode.ShouldContain("the provider's display name");
    [Fact] void should_keep_quotes_as_written() => _generatedCode.ShouldContain("\"the old one\"");
    [Fact] void should_keep_an_ampersand_as_written() => _generatedCode.ShouldContain("& the new");
    [Fact] void should_not_html_escape_the_documentation() => _generatedCode.ShouldNotContain("&#x27;");
}
