// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Xml.Linq;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Utilities for extracting and converting XML documentation to JSDoc format.
/// </summary>
public static class XmlDocumentation
{
    static readonly Dictionary<string, XDocument> _loadedDocuments = [];

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

        return paramElement.Value.Trim();
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

        return summary.Value.Trim();
    }

    static XDocument? GetXmlDocument(Assembly assembly)
    {
        var assemblyLocation = assembly.Location;
        if (string.IsNullOrEmpty(assemblyLocation)) return null;

        var xmlPath = Path.ChangeExtension(assemblyLocation, ".xml");
        if (!File.Exists(xmlPath)) return null;

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

    static string ConvertXmlToJsDoc(XElement element)
    {
        var summary = element.Element("summary");
        if (summary is null) return string.Empty;

        var lines = summary.Value
            .Split(['\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line));

        return string.Join(' ', lines);
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
