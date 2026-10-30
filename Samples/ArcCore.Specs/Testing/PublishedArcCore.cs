// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Samples.ArcCore.Specs.Testing;

/// <summary>
/// Owns a published ArcCore process and its HTTP client.
/// </summary>
public sealed class PublishedArcCore
{
    readonly Process _process;
    int? _exitCode;
    readonly ConcurrentQueue<string> _output = new();
    readonly TaskCompletionSource<string> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    PublishedArcCore(ProcessStartInfo startInfo)
    {
        _process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _process.OutputDataReceived += CaptureOutput;
        _process.ErrorDataReceived += CaptureOutput;
        _process.Exited += (_, _) => _ready.TrySetException(new PublishedArcCoreDidNotStart(_process.ExitCode, Output));
    }

    /// <summary>
    /// Gets the client for the prefix reported by ArcCore.
    /// </summary>
    public HttpClient Client { get; private set; } = null!;

    /// <summary>
    /// Gets all captured standard output and standard error.
    /// </summary>
    public string Output => string.Join(Environment.NewLine, _output);

    /// <summary>
    /// Starts the configured executable and awaits its readiness signal.
    /// </summary>
    /// <returns>The running executable and its client.</returns>
    /// <exception cref="PublishedArcCoreNotConfigured">The executable path is missing or invalid.</exception>
    /// <exception cref="PublishedArcCoreDidNotStart">The process exits or fails to signal readiness.</exception>
    public static async Task<PublishedArcCore> Start()
    {
        var executable = Environment.GetEnvironmentVariable("CRATIS_ARC_PUBLISHED_ARCCORE");
        if (string.IsNullOrWhiteSpace(executable) || !Path.IsPathFullyQualified(executable) || !File.Exists(executable))
        {
            throw new PublishedArcCoreNotConfigured();
        }

        var requestedUrl = GetAvailableUrl();
        var arcCore = new PublishedArcCore(CreateStartInfo(executable, requestedUrl));
        arcCore._process.Start();
        arcCore._process.BeginOutputReadLine();
        arcCore._process.BeginErrorReadLine();
        try
        {
            var reportedUrl = await arcCore._ready.Task.WaitAsync(TimeSpan.FromSeconds(30));
            if (!string.Equals(requestedUrl, reportedUrl, StringComparison.Ordinal))
            {
                arcCore._output.Enqueue($"ApplicationUrl binding mismatch: requested {requestedUrl}, reported {reportedUrl}");
            }

            arcCore.Client = new HttpClient { BaseAddress = new Uri(reportedUrl) };
            return arcCore;
        }
        catch (Exception error) when (error is TimeoutException or PublishedArcCoreDidNotStart)
        {
            await arcCore.Stop();
            throw new PublishedArcCoreDidNotStart(arcCore._exitCode, arcCore.Output);
        }
    }

    /// <summary>
    /// Stops the process tree and saves output when a log path is configured.
    /// </summary>
    /// <returns>A task that completes after process cleanup.</returns>
    public async Task Stop()
    {
        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
        }

        await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
        _exitCode = _process.ExitCode;
        _process.Dispose();
        Client?.Dispose();
        var logPath = Environment.GetEnvironmentVariable("CRATIS_ARC_PUBLISHED_ARCCORE_LOG");
        if (!string.IsNullOrWhiteSpace(logPath))
        {
            await File.AppendAllTextAsync(logPath, Output + Environment.NewLine);
        }
    }

    static string GetAvailableUrl()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return $"http://127.0.0.1:{port}/";
    }

    static ProcessStartInfo CreateStartInfo(string executable, string requestedUrl)
    {
        var startInfo = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(executable)
        };
        startInfo.ArgumentList.Add($"--Cratis:Arc:Hosting:ApplicationUrl={requestedUrl}");
        startInfo.ArgumentList.Add("--Logging:LogLevel:Default=Information");
        startInfo.ArgumentList.Add("--Logging:LogLevel:Cratis=Information");
        startInfo.Environment["DOTNET_ENVIRONMENT"] = "Production";
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        return startInfo;
    }

    void CaptureOutput(object sender, DataReceivedEventArgs args)
    {
        if (args.Data is not { } line)
        {
            return;
        }

        _output.Enqueue(line);
        const string marker = "HttpListener started on:";
        var markerIndex = line.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex >= 0)
        {
            var prefix = line[(markerIndex + marker.Length)..].Trim().Split(',', StringSplitOptions.TrimEntries)[0];
            _ready.TrySetResult(prefix.Replace("+", "127.0.0.1", StringComparison.Ordinal).Replace("*", "127.0.0.1", StringComparison.Ordinal));
        }
    }
}
