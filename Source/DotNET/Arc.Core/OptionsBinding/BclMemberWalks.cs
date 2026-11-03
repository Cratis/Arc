// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The public instance properties of <see cref="string"/>, <see cref="TimeSpan"/> and <see cref="Uri"/>, which the
/// reflection binder walks when a value of one of these types receives child keys. Every one is get-only, so a walk converts
/// the configured value, which can fail, and discards it. The order is the metadata order, identical on .NET 8, 9 and 10.
/// </summary>
internal static class BclMemberWalks
{
    /// <summary>
    /// Gets the members of <see cref="string"/>.
    /// </summary>
    /// <returns>The members.</returns>
    internal static Member[] String() =>
    [
        Member.Of<string, int>("Length", Leaves.Int, static _ => _.Length)
    ];

    /// <summary>
    /// Gets the members of <see cref="TimeSpan"/>.
    /// </summary>
    /// <returns>The members.</returns>
    internal static Member[] TimeSpan() =>
    [
        Member.Of<TimeSpan, long>("Ticks", Leaves.Long, static _ => _.Ticks),
        Member.Of<TimeSpan, int>("Days", Leaves.Int, static _ => _.Days),
        Member.Of<TimeSpan, int>("Hours", Leaves.Int, static _ => _.Hours),
        Member.Of<TimeSpan, int>("Milliseconds", Leaves.Int, static _ => _.Milliseconds),
        Member.Of<TimeSpan, int>("Microseconds", Leaves.Int, static _ => _.Microseconds),
        Member.Of<TimeSpan, int>("Nanoseconds", Leaves.Int, static _ => _.Nanoseconds),
        Member.Of<TimeSpan, int>("Minutes", Leaves.Int, static _ => _.Minutes),
        Member.Of<TimeSpan, int>("Seconds", Leaves.Int, static _ => _.Seconds),
        Member.Of<TimeSpan, double>("TotalDays", Leaves.Double, static _ => _.TotalDays),
        Member.Of<TimeSpan, double>("TotalHours", Leaves.Double, static _ => _.TotalHours),
        Member.Of<TimeSpan, double>("TotalMilliseconds", Leaves.Double, static _ => _.TotalMilliseconds),
        Member.Of<TimeSpan, double>("TotalMicroseconds", Leaves.Double, static _ => _.TotalMicroseconds),
        Member.Of<TimeSpan, double>("TotalNanoseconds", Leaves.Double, static _ => _.TotalNanoseconds),
        Member.Of<TimeSpan, double>("TotalMinutes", Leaves.Double, static _ => _.TotalMinutes),
        Member.Of<TimeSpan, double>("TotalSeconds", Leaves.Double, static _ => _.TotalSeconds)
    ];

    /// <summary>
    /// Gets the members of <see cref="Uri"/>. <c>Segments</c> is left out: it is a get-only array, which the binder never
    /// reads or assigns.
    /// </summary>
    /// <returns>The members.</returns>
    internal static Member[] Uri() =>
    [
        Member.Of<Uri, string>("AbsolutePath", Leaves.String, static _ => _.AbsolutePath),
        Member.Of<Uri, string>("AbsoluteUri", Leaves.String, static _ => _.AbsoluteUri),
        Member.Of<Uri, string>("LocalPath", Leaves.String, static _ => _.LocalPath),
        Member.Of<Uri, string>("Authority", Leaves.String, static _ => _.Authority),
        Member.Of<Uri, UriHostNameType>("HostNameType", Leaves.UriHostNameType, static _ => _.HostNameType),
        Member.Of<Uri, bool>("IsDefaultPort", Leaves.Bool, static _ => _.IsDefaultPort),
        Member.Of<Uri, bool>("IsFile", Leaves.Bool, static _ => _.IsFile),
        Member.Of<Uri, bool>("IsLoopback", Leaves.Bool, static _ => _.IsLoopback),
        Member.Of<Uri, string>("PathAndQuery", Leaves.String, static _ => _.PathAndQuery),
        Member.Of<Uri, bool>("IsUnc", Leaves.Bool, static _ => _.IsUnc),
        Member.Of<Uri, string>("Host", Leaves.String, static _ => _.Host),
        Member.Of<Uri, int>("Port", Leaves.Int, static _ => _.Port),
        Member.Of<Uri, string>("Query", Leaves.String, static _ => _.Query),
        Member.Of<Uri, string>("Fragment", Leaves.String, static _ => _.Fragment),
        Member.Of<Uri, string>("Scheme", Leaves.String, static _ => _.Scheme),
        Member.Of<Uri, string>("OriginalString", Leaves.String, static _ => _.OriginalString),
        Member.Of<Uri, string>("DnsSafeHost", Leaves.String, static _ => _.DnsSafeHost),
        Member.Of<Uri, string>("IdnHost", Leaves.String, static _ => _.IdnHost),
        Member.Of<Uri, bool>("IsAbsoluteUri", Leaves.Bool, static _ => _.IsAbsoluteUri),
        Member.Of<Uri, bool>("UserEscaped", Leaves.Bool, static _ => _.UserEscaped),
        Member.Of<Uri, string>("UserInfo", Leaves.String, static _ => _.UserInfo)
    ];
}
