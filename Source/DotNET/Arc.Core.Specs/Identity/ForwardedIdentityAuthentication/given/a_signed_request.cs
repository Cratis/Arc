// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication.given;

public class a_signed_request : Specification
{
    protected ArcOptions _options;
    protected IHttpRequestContext _context;
    protected Dictionary<string, string> _headers;
    protected Dictionary<string, string[]> _rawHeaders;
    private protected ForwardedIdentityReplayCache _replays;
    protected TimeProvider _clock;

    void Establish() => ResetRequest();

    protected void ResetRequest()
    {
        _options = ForwardedIdentityForSpecs.Options();
        _headers = ForwardedIdentityForSpecs.Headers();
        _headers[ForwardedIdentityForSpecs.SignatureHeader] = ForwardedIdentityForSpecs.Envelope;
        _rawHeaders = _headers.ToDictionary(pair => pair.Key, pair => new[] { pair.Value }, StringComparer.OrdinalIgnoreCase);
        _context = Substitute.For<IHttpRequestContext, ICanReadForwardedIdentityRequest>();
        _context.Method.Returns("POST");
        _context.Path.Returns("/base/a%2Fb");
        _context.Headers.Returns(_headers);
        _context.Items.Returns(new Dictionary<object, object?>());
        var raw = (ICanReadForwardedIdentityRequest)_context;
        raw.RawTarget.Returns("/base/a%2fb?x=1");
        raw.HeaderNames.Returns(_ => _rawHeaders.Keys);
        raw.GetHeaderValues(Arg.Any<string>()).Returns(call => _rawHeaders.TryGetValue(call.Arg<string>(), out var values) ? values : []);
        _replays = new();
        _clock = Substitute.For<TimeProvider>();
        _clock.GetUtcNow().Returns(DateTimeOffset.FromUnixTimeSeconds(1700000000));
    }

    private protected ForwardedIdentityAuthenticationResult Authenticate() => ForwardedIdentityAuthentication.Authenticate(_context, _options, "specs", _context.Path, _clock, _replays);

    protected void Resign(long timestamp = 1700000000)
    {
        _headers[ForwardedIdentityForSpecs.SignatureHeader] = ForwardedIdentityForSpecs.Sign(_headers, timestamp: timestamp);
        foreach (var pair in _headers)
        {
            _rawHeaders[pair.Key] = [pair.Value];
        }
    }
}
