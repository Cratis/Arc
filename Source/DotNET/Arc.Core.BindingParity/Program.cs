// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text;
using Cratis.Arc.OptionsBinding;

// Exit codes: 0 every scenario matched, 1 a mismatch, 2 could not run.
const int Mismatch = 1;
const int CouldNotRun = 2;

var report = new StringBuilder();
string? reportPath = null;
try
{
    reportPath = ReportPath(args);
    return Run(report, reportPath);
}
catch (Exception exception)
{
    Console.Error.WriteLine($"could not run: {exception}");
    return CouldNotRun;
}

static int Run(StringBuilder report, string? reportPath)
{
    var expectedMajor = ExpectedRuntimeMajor();
    report.AppendLine($"runtime {Environment.Version} (expected major {expectedMajor}), configuration binder {typeof(Microsoft.Extensions.Configuration.ConfigurationBinder).Assembly.GetName().Version}");

    var scenarios = typeof(given_a_parity_run).Assembly.GetTypes()
        .Where(_ => !_.IsAbstract && _.IsSubclassOf(typeof(given_a_parity_run)))
        .OrderBy(_ => _.Name, StringComparer.Ordinal)
        .ToArray();

    if (Environment.Version.Major != expectedMajor || scenarios.Length == 0)
    {
        report.AppendLine($"could not run: {scenarios.Length} scenarios on runtime {Environment.Version}");
        return Finish(report, reportPath, CouldNotRun);
    }

    var because = typeof(given_a_parity_run).GetMethod("Because", BindingFlags.NonPublic | BindingFlags.Instance)!;
    var runField = typeof(given_a_parity_run).GetField("_run", BindingFlags.NonPublic | BindingFlags.Instance)!;
    var mismatches = 0;
    foreach (var scenario in scenarios)
    {
        var instance = Activator.CreateInstance(scenario)!;
        because.Invoke(instance, null);
        var run = (ParityRun)runField.GetValue(instance)!;
        var same = run.ReflectionFreeSnapshot == run.ReflectionSnapshot && run.ReflectionFreeFailure == run.ReflectionFailure;
        report.AppendLine($"{(same ? "PASS" : "FAIL")} {scenario.Name}");
        if (!same)
        {
            mismatches++;
            AppendDifference(report, run);
        }
    }

    report.AppendLine($"{scenarios.Length} scenarios, {mismatches} mismatched");
    return Finish(report, reportPath, mismatches == 0 ? 0 : Mismatch);
}

static int Finish(StringBuilder report, string? reportPath, int exitCode)
{
    Console.Write(report);
    if (reportPath is not null)
    {
        File.WriteAllText(reportPath, report.ToString());
    }

    return exitCode;
}

static string? ReportPath(string[] args)
{
    return args.Length switch
    {
        0 => null,
        2 when args[0] == "--report" => args[1],
        _ => throw new ArgumentException("Usage: Arc.Core.BindingParity [--report <file>]")
    };
}

static int ExpectedRuntimeMajor()
{
    var framework = typeof(Program).Assembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()!.FrameworkName;
    return int.Parse(framework[(framework.IndexOf("Version=v", StringComparison.Ordinal) + "Version=v".Length)..].Split('.')[0]);
}

static void AppendDifference(StringBuilder report, ParityRun run)
{
    report.AppendLine($"  reflection failure: {run.ReflectionFailure}")
        .AppendLine($"  table failure:      {run.ReflectionFreeFailure}");
    var reflection = run.ReflectionSnapshot.Split('\n');
    var table = run.ReflectionFreeSnapshot.Split('\n');
    foreach (var line in reflection.Except(table).Take(5))
    {
        report.AppendLine($"  only reflection: {line}");
    }

    foreach (var line in table.Except(reflection).Take(5))
    {
        report.AppendLine($"  only table:      {line}");
    }
}
