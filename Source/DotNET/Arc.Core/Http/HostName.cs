// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http;

/// <summary>
/// Helpers for working with the host part of an HTTP request.
/// </summary>
internal static class HostName
{
    /// <summary>
    /// Removes the port from a host value, leaving IPv6 literals intact.
    /// </summary>
    /// <param name="host">The host value, optionally including a port.</param>
    /// <returns>The host without its port.</returns>
    /// <remarks>
    /// An IPv6 literal is written with brackets when it carries a port (<c>[::1]:5000</c>) and contains colons of its
    /// own, so splitting on the first colon mangles it. A bracketed value is cut after its closing bracket and a
    /// bracket-less value with more than one colon is a bare IPv6 literal that has no port to remove.
    /// </remarks>
    internal static string WithoutPort(string host)
    {
        if (host.Length == 0)
        {
            return host;
        }

        if (host[0] == '[')
        {
            var closingBracket = host.IndexOf(']');
            return closingBracket < 0 ? host : host[..(closingBracket + 1)];
        }

        var portSeparator = host.IndexOf(':');
        if (portSeparator < 0)
        {
            return host;
        }

        return host.IndexOf(':', portSeparator + 1) < 0 ? host[..portSeparator] : host;
    }
}
