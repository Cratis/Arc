// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.given;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Cratis.Arc.Mcp.for_McpAdmission;

public class when_origins_are_refused : Specification
{
    const string Credential = "dpat_spec_not_a_real_credential";
    const string Argument = "private-tool-argument";
    LogRecorder<McpAdmission> _logger;
    McpAdmission _admission;
    HttpContext _refused;
    string _body;
    string _foreign;
    string _multiple;
    string _malformed;
    string _sanitized;
    int _allowedLogs;

    void Establish()
    {
        _logger = new();
        var options = new McpOptions();
        options.AllowedOrigins.Add("http://localhost:6274");
        _admission = new(Options.Create(options), null!, _logger);
    }

    async Task Because()
    {
        _refused = await Send("http://localhost:6275");
        _refused.Response.Body.Position = 0;
        _body = await new StreamReader(_refused.Response.Body).ReadToEndAsync();
        _foreign = _logger.Entries[^1].Message;
        await Send(new StringValues(["http://localhost:6274", "https://other.example.test"]));
        _multiple = _logger.Entries[^1].Message;
        await Send("null");
        _malformed = _logger.Entries[^1].Message;
        await Send("https://example.test/\r\n" + new string('x', 300));
        _sanitized = (string)_logger.Entries[^1].Values[0]!;
        var before = _logger.Entries.Count;
        await Send("http://localhost:6274");
        await Send(StringValues.Empty);
        _allowedLogs = _logger.Entries.Count - before;
    }

    [Fact] void should_keep_the_forbidden_status() => _refused.Response.StatusCode.ShouldEqual(StatusCodes.Status403Forbidden);
    [Fact] void should_keep_the_refusal_body() => _body.ShouldEqual("{\"error\":\"origin_not_allowed\"}");
    [Fact] void should_name_the_refused_origin() => _foreign.ShouldContain("http://localhost:6275");
    [Fact] void should_name_the_setting_to_change() => _foreign.ShouldContain("McpOptions.AllowedOrigins");
    [Fact] void should_distinguish_multiple_origins() => _multiple.ShouldContain("multiple Origin values");
    [Fact] void should_distinguish_a_malformed_origin() => _malformed.ShouldContain("malformed Origin");
    [Fact] void should_bound_the_logged_origin() => _sanitized.Length.ShouldEqual(200);
    [Fact] void should_remove_control_characters() => _sanitized.Any(char.IsControl).ShouldBeFalse();
    [Fact] void should_only_log_refused_origins() => _allowedLogs.ShouldEqual(0);
    [Fact] void should_log_at_debug_level() => _logger.Entries.Select(_ => _.Level).Distinct().ShouldContainOnly(LogLevel.Debug);
    [Fact] void should_not_log_credentials() => LoggedValues().Any(_ => _.Contains(Credential, StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_log_arguments() => LoggedValues().Any(_ => _.Contains(Argument, StringComparison.Ordinal)).ShouldBeFalse();

    IEnumerable<string> LoggedValues() => _logger.Entries.SelectMany(_ => _.Values.Select(value => value?.ToString() ?? string.Empty).Append(_.Message));

    async Task<HttpContext> Send(StringValues origins)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Origin = origins;
        context.Request.Headers.Authorization = $"Bearer {Credential}";
        context.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(Argument));
        context.Response.Body = new MemoryStream();
        await _admission.Invoke(context, _ => Task.CompletedTask);
        return context;
    }
}
