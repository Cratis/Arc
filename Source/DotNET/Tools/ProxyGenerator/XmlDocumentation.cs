// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Utilities for extracting and converting XML documentation to JSDoc format.
/// </summary>
public static class XmlDocumentation
{
    static readonly Dictionary<string, XDocument> _loadedDocuments = [];
    static readonly object _loadedDocumentsLock = new();

    /// <summary>
    /// Get the XML documentation for a member.
    /// </summary>
    /// <param name="member">The member to get documentation for.</param>
    /// <returns>The XML documentation as JSDoc, or null if not found.</returns>
    public static string? GetDocumentation(this MemberInfo member)
    {
        var assembly = member.DeclaringType?.Assembly ?? member.Module.Assembly;
        var xmlDoc = GetXmlDocument(assembly);
        if (xmlDoc is null) return null;

        var memberName = GetMemberName(member);
        var element = xmlDoc.Descendants("member")
            .FirstOrDefault(m => m.Attribute("name")?.Value == memberName);

        if (element is null) return null;

        return ConvertXmlToJsDoc(element);
    }

    /// <summary>
    /// Get the XML documentation for a type.
    /// </summary>
    /// <param name="type">The type to get documentation for.</param>
    /// <returns>The XML documentation as JSDoc, or null if not found.</returns>
    public static string? GetDocumentation(this Type type)
    {
        var assembly = type.Assembly;
        var xmlDoc = GetXmlDocument(assembly);
        if (xmlDoc is null) return null;

        var memberName = GetTypeName(type);
        var element = xmlDoc.Descendants("member")
            .FirstOrDefault(m => m.Attribute("name")?.Value == memberName);

        if (element is null) return null;

        return ConvertXmlToJsDoc(element);
    }

    /// <summary>
    /// Get the XML documentation for a parameter.
    /// </summary>
    /// <param name="parameter">The parameter to get documentation for.</param>
    /// <returns>The XML documentation as JSDoc, or null if not found.</returns>
    public static string? GetDocumentation(this ParameterInfo parameter)
    {
        if (parameter.Member is not MethodInfo method) return null;

        var assembly = method.DeclaringType?.Assembly ?? method.Module.Assembly;
        var xmlDoc = GetXmlDocument(assembly);
        if (xmlDoc is null) return null;

        var memberName = GetMemberName(method);
        var methodElement = xmlDoc.Descendants("member")
            .FirstOrDefault(m => m.Attribute("name")?.Value == memberName);

        if (methodElement is null) return null;

        var paramElement = methodElement.Descendants("param")
            .FirstOrDefault(p => p.Attribute("name")?.Value == parameter.Name);

        if (paramElement is null) return null;

        return Render(paramElement);
    }

    /// <summary>
    /// Get the XML documentation for a property.
    /// </summary>
    /// <param name="property">The property to get documentation for.</param>
    /// <returns>The XML documentation as JSDoc, or null if not found.</returns>
    public static string? GetDocumentation(this PropertyInfo property)
    {
        var assembly = property.DeclaringType?.Assembly ?? property.Module.Assembly;
        var xmlDoc = GetXmlDocument(assembly);
        if (xmlDoc is null) return null;

        var memberName = GetMemberName(property);
        var element = xmlDoc.Descendants("member")
            .FirstOrDefault(m => m.Attribute("name")?.Value == memberName);

        if (element is null) return null;

        var summary = element.Element("summary");
        if (summary is null) return null;

        return Render(summary);
    }

    static XDocument? GetXmlDocument(Assembly assembly)
    {
        var assemblyLocation = assembly.Location;
        if (string.IsNullOrEmpty(assemblyLocation)) return null;

        var xmlPath = Path.ChangeExtension(assemblyLocation, ".xml");
        if (!File.Exists(xmlPath)) return null;

        lock (_loadedDocumentsLock)
        {
            if (_loadedDocuments.TryGetValue(xmlPath, out var doc))
            {
                return doc;
            }

            try
            {
                doc = XDocument.Load(xmlPath);
                _loadedDocuments[xmlPath] = doc;
                return doc;
            }
            catch
            {
                return null;
            }
        }
    }

    static string ConvertXmlToJsDoc(XElement element)
    {
        var summary = element.Element("summary");
        return summary is null ? string.Empty : Render(summary);
    }

    /// <summary>
    /// Render a documentation element as the single line of JSDoc prose the templates emit.
    /// </summary>
    /// <param name="element">The documentation element to render.</param>
    /// <returns>The rendered prose.</returns>
    /// <remarks>
    /// Reading <see cref="XElement.Value"/> instead would concatenate the text content of the element and all its
    /// descendants - which silently deletes every element whose payload lives in an attribute rather than in a text
    /// child. That is exactly the set the .NET documentation conventions ask authors to use: a self-closing
    /// <c>see cref</c>, <c>paramref</c>, <c>typeparamref</c> or <c>see langword</c> has no text child at all, so the
    /// prose fuses around the hole and reads as a finished sentence that has lost its subject. The better the source
    /// is documented, the more of it disappears - and the artifact is generated, so nobody sees it happen.
    /// <para>
    /// All three documentation entry points route through here, so a param, a property summary and a type summary
    /// can no longer be rendered three different ways.
    /// </para>
    /// </remarks>
    static string Render(XElement element)
    {
        var builder = new StringBuilder();
        AppendNodes(element, builder);
        return CollapseWhitespace(builder.ToString());
    }

    static void AppendNodes(XElement element, StringBuilder builder)
    {
        foreach (var node in element.Nodes())
        {
            switch (node)
            {
                case XText text:
                    builder.Append(text.Value);
                    break;

                case XElement child:
                    AppendElement(child, builder);
                    break;
            }
        }
    }

    static void AppendElement(XElement element, StringBuilder builder)
    {
        switch (element.Name.LocalName)
        {
            case "see":
            case "seealso":
                AppendReference(element, builder);
                break;

            case "paramref":
            case "typeparamref":
                AppendCode(element.Attribute("name")?.Value, builder);
                break;

            case "c":
            case "code":
                AppendCode(element.Value, builder);
                break;

            default:
                // para, b, i, list, and anything else the author wrote: the element itself carries no meaning the
                // one-line JSDoc can express, but its prose does, so keep walking rather than dropping it.
                AppendNodes(element, builder);
                break;
        }
    }

    static void AppendReference(XElement element, StringBuilder builder)
    {
        // An explicit label wins - the author already said how they wanted it read.
        var label = element.Value.Trim();
        if (!string.IsNullOrEmpty(label))
        {
            builder.Append(label);
            return;
        }

        if (element.Attribute("langword")?.Value is { Length: > 0 } langword)
        {
            AppendCode(langword, builder);
            return;
        }

        if (element.Attribute("cref")?.Value is { Length: > 0 } cref)
        {
            builder.Append("{@link ").Append(SimpleNameFrom(cref)).Append('}');
            return;
        }

        if (element.Attribute("href")?.Value is { Length: > 0 } href)
        {
            builder.Append(href);
        }
    }

    static void AppendCode(string? value, StringBuilder builder)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;

        builder.Append('`').Append(trimmed).Append('`');
    }

    /// <summary>
    /// Reduce a documentation cref to the name a reader recognizes.
    /// </summary>
    /// <param name="cref">The cref as the compiler wrote it, e.g. <c>T:Some.Namespace.Widget</c>.</param>
    /// <returns>The simple name, e.g. <c>Widget</c>.</returns>
    static string SimpleNameFrom(string cref)
    {
        var name = cref;

        // The compiler prefixes a resolved cref with its member kind - T:, M:, P:, F:, E:, N: - and writes !: when
        // it could not resolve one at all.
        if (name.Length > 2 && name[1] == ':')
        {
            name = name[2..];
        }

        // A method cref carries its parameter list, which is not part of the name.
        var parameters = name.IndexOf('(');
        if (parameters >= 0)
        {
            name = name[..parameters];
        }

        var lastSegment = name.LastIndexOf('.');
        if (lastSegment >= 0)
        {
            name = name[(lastSegment + 1)..];
        }

        // A generic type is written with its arity, as in Widget`1.
        var arity = name.IndexOf('`');
        return arity >= 0 ? name[..arity] : name;
    }

    /// <summary>
    /// Collapse every run of whitespace to a single space.
    /// </summary>
    /// <param name="value">The rendered prose.</param>
    /// <returns>The prose as one line.</returns>
    /// <remarks>
    /// The templates emit documentation on a single line, so the newlines an author wrapped their comment at have to
    /// go. Doing it here rather than per call site is also what keeps a rendered-away element from leaving the two
    /// spaces that surrounded it behind as a visible seam.
    /// </remarks>
    static string CollapseWhitespace(string value)
    {
        var builder = new StringBuilder(value.Length);
        var pendingSpace = false;

        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character))
            {
                // Held rather than written, so a run of whitespace costs one space and a trailing run costs none.
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    static string GetMemberName(MemberInfo member)
    {
        return member switch
        {
            MethodInfo method => GetMethodName(method),
            PropertyInfo property => $"P:{property.DeclaringType?.FullName ?? property.DeclaringType?.Name}.{property.Name}",
            Type type => GetTypeName(type),
            _ => string.Empty
        };
    }

    static string GetMethodName(MethodInfo method)
    {
        var parameters = method.GetParameters();
        var parameterTypes = string.Join(',', parameters.Select(p => GetTypeFullName(p.ParameterType)));
        var declaringType = method.DeclaringType?.FullName ?? string.Empty;

        if (parameters.Length == 0)
        {
            return $"M:{declaringType}.{method.Name}";
        }

        return $"M:{declaringType}.{method.Name}({parameterTypes})";
    }

    static string GetTypeName(Type type)
    {
        return $"T:{type.FullName ?? type.Name}";
    }

    static string GetTypeFullName(Type type)
    {
        if (type.IsGenericType)
        {
            var genericTypeName = type.GetGenericTypeDefinition().FullName;
            if (genericTypeName is not null)
            {
                var backTickIndex = genericTypeName.IndexOf('`');
                genericTypeName = backTickIndex >= 0 ? genericTypeName[..backTickIndex] : genericTypeName;
            }
            else
            {
                genericTypeName = type.Name;
            }
            var genericArgs = string.Join(',', type.GetGenericArguments().Select(GetTypeFullName));
            return $"{genericTypeName}{{{genericArgs}}}";
        }

        return type.FullName ?? type.Name;
    }
}
