// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Identity.for_IdentityProviderResultHandler;

public class TestRequestCookieCollection : IRequestCookieCollection
{
    private readonly Dictionary<string, string> _cookies = new();

    public TestRequestCookieCollection()
    {
    }

    public TestRequestCookieCollection(Dictionary<string, string> cookies)
    {
        _cookies = cookies;
    }

    public void Add(string key, string value)
    {
        _cookies[key] = value;
    }

    public string? this[string key] => _cookies.TryGetValue(key, out var value) ? value : null;

    public int Count => _cookies.Count;

    public ICollection<string> Keys => _cookies.Keys;

    public bool ContainsKey(string key) => _cookies.ContainsKey(key);

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _cookies.GetEnumerator();

    public bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => _cookies.TryGetValue(key, out value);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}