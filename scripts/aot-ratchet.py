#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
"""Ratchet for the trim and NativeAOT analyzer diagnostics of the shipped Arc libraries (#2859, part of #2204).

Arc is not trim or AOT compatible yet, so failing on every diagnostic would block every pull request. Instead
this builds the projects with the analyzers switched on (CratisAotAnalysis=true, honored only by the projects
importing Source/DotNET/AotAnalysis.props - Arc.Core, Arc, MongoDB, EntityFrameworkCore and Chronicle) and holds
what they report to the checked-in baseline in Source/DotNET/aot-baseline.json. The covered projects are the ones
that import the props: importing it is what puts a project under the ratchet, and the script finds them itself.

- a diagnostic is keyed by code, repository-relative file and target framework - not by line or message, so
  edits that only move code do not churn the baseline - and counted per key;
- a key that is new, or whose count went up, fails: the change added trim/AOT unsafe code;
- a key that is gone, or whose count went down, also fails: the change fixed diagnostics, and the baseline has
  to shrink in the same pull request so the improvement cannot quietly be given back later;
- suppressions under Source/DotNET (UnconditionalSuppressMessage, SuppressMessage for the Trimming, AOT and
  SingleFile categories, '#pragma warning disable IL....', and IL codes in NoWarn or .editorconfig severities)
  are counted per file and held to the baseline the same way. Unsafe paths are replaced, not suppressed.

Usage:
  python3 scripts/aot-ratchet.py              build, then compare with the baseline
  python3 scripts/aot-ratchet.py --update     build, then rewrite the baseline from what the build reports
  python3 scripts/aot-ratchet.py --sarif DIR  keep the build's SARIF logs in DIR
  python3 scripts/aot-ratchet.py --sarif DIR --no-build
                                              compare using SARIF logs a previous run kept in DIR
  python3 scripts/aot-ratchet.py --self-test  check the comparison logic without building

Exit codes: 0 matches the baseline, 1 differs from it, 2 the build or the script itself failed.
"""

import argparse
import contextlib
import glob
import io
import json
import os
import re
import subprocess
import sys
import tempfile
from collections import Counter
from urllib.parse import unquote, urlparse

ROOT = os.path.dirname(os.path.dirname(os.path.realpath(__file__)))
BASELINE = os.path.join(ROOT, 'Source', 'DotNET', 'aot-baseline.json')
SOURCE = os.path.join(ROOT, 'Source', 'DotNET')

# A project is held to the ratchet by importing this file; it is what switches the analyzers on.
PROPS_IMPORT = 'AotAnalysis.props'
# The analyzers need a target framework compatible with net8.0; the props file leaves the others unanalyzed.
MINIMUM_MAJOR_VERSION = 8

SUPPRESSION_IN_CODE = re.compile(
    r'UnconditionalSuppressMessage'
    r'|(?<!Unconditional)SuppressMessage(?:Attribute)?\s*\(\s*(?:category\s*:\s*)?"(?:Trimming|AOT|SingleFile)"'
    r'|#\s*pragma\s+warning\s+disable\b[^\n]*\bIL\d{4}'
    # A bare `#pragma warning disable` turns off every warning, trim and AOT ones included.
    r'|#\s*pragma\s+warning\s+disable[ \t]*(?://[^\n]*)?$', re.MULTILINE)
SUPPRESSION_IN_CONFIGURATION = re.compile(
    r'<NoWarn>[^<]*\bIL\d{4}|dotnet_diagnostic\.IL\d{4}\.severity')
CONFIGURATION_FILES = ('.csproj', '.props', '.targets', '.editorconfig', '.globalconfig')
# Repository-root files that also configure Arc.Core and Arc, scanned without descending into the tree.
ROOT_CONFIGURATION = ('.editorconfig', '.globalconfig', 'Directory.Build.props', 'Directory.Build.targets',
                      'Directory.Packages.props')
SKIPPED_DIRECTORIES = {'bin', 'obj', 'node_modules', '.git'}
SARIF_NAME = re.compile(r'^(?P<project>.+)\.(?P<tfm>net[^.]*(?:\.\d+)?)\.sarif$')


def relative(path):
    """Repository-relative path with forward slashes; the path as reported when it is outside the repository."""
    normalized = os.path.normpath(path.strip())
    if os.path.isabs(normalized):
        inside = os.path.relpath(os.path.realpath(normalized), ROOT)
        if not inside.startswith('..'):
            normalized = inside
    return normalized.replace(os.sep, '/')


def file_of(result, fallback):
    for location in result.get('locations') or []:
        uri = location.get('physicalLocation', {}).get('artifactLocation', {}).get('uri')
        if uri:
            parsed = urlparse(uri)
            path = unquote(parsed.path) if parsed.scheme == 'file' else unquote(uri)
            # file:///C:/x parses to /C:/x on Windows.
            if re.match(r'^/[A-Za-z]:[/\\]', path):
                path = path[1:]
            return relative(path)
    return fallback


def line_of(result):
    for location in result.get('locations') or []:
        region = location.get('physicalLocation', {}).get('region', {})
        if region:
            return f"{region.get('startLine', 0)},{region.get('startColumn', 0)}"
    return '0,0'


def collect(logs):
    """Unsuppressed IL diagnostics from (project, tfm, sarif) triples, as {key: count} and {key: [examples]}."""
    counts = Counter()
    examples = {}
    for project, tfm, sarif in logs:
        for run in sarif.get('runs', []):
            for result in run.get('results', []):
                code = result.get('ruleId', '')
                if not re.fullmatch(r'IL\d{4}', code) or result.get('suppressions'):
                    continue
                file = file_of(result, f'{project} (no source location)')
                key = f'{code}|{file}|{tfm}'
                counts[key] += 1
                message = result.get('message', {}).get('text', '')
                examples.setdefault(key, []).append(f'{file}({line_of(result)}): {code}: {message} [{tfm}]')
    return dict(counts), examples


def load_json(path):
    """Read a JSON file; exit 2 (could not run) rather than 1 (differs) when it is unreadable."""
    try:
        with open(path, encoding='utf-8-sig') as handle:
            return json.load(handle)
    except (OSError, ValueError) as error:
        print(f'Could not read {relative(path)}: {error}', file=sys.stderr)
        sys.exit(2)


def covered_projects():
    """Sorted repository-relative paths of the projects under Source/DotNET that import AotAnalysis.props."""
    found = []
    for directory, directories, files in os.walk(SOURCE):
        directories[:] = sorted(d for d in directories if d not in SKIPPED_DIRECTORIES)
        for name in sorted(files):
            if not name.endswith('.csproj'):
                continue
            path = os.path.join(directory, name)
            with open(path, encoding='utf-8', errors='replace') as handle:
                if PROPS_IMPORT in handle.read():
                    found.append(relative(path))
    return sorted(found)


def analyzed_frameworks(target_frameworks):
    """The target frameworks of a semicolon separated list that the analyzers run for (net8.0 or newer)."""
    frameworks = []
    for framework in target_frameworks.split(';'):
        match = re.match(r'^net(\d+)\.\d+', framework.strip())
        if match and int(match[1]) >= MINIMUM_MAJOR_VERSION:
            frameworks.append(framework.strip())
    return frameworks


def project_frameworks(project):
    """The analyzed target frameworks of a project as MSBuild evaluates them for the Release build; exit 2 on failure."""
    command = ['dotnet', 'msbuild', os.path.join(ROOT, project), '-getProperty:TargetFrameworks',
               '-p:Configuration=Release', '-nologo']
    result = subprocess.run(command, cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
    frameworks = analyzed_frameworks(result.stdout.strip()) if result.returncode == 0 else []
    if not frameworks:
        print(result.stdout + result.stderr)
        print(f'Could not determine the target frameworks of {project}.', file=sys.stderr)
        sys.exit(2)
    return frameworks


def expected_logs(frameworks_by_project):
    """The (project name, target framework) pairs that must each have written a SARIF log."""
    return {(os.path.splitext(os.path.basename(project))[0], framework)
            for project, frameworks in frameworks_by_project.items() for framework in frameworks}


def read_logs(directory, expected):
    logs = []
    for path in sorted(glob.glob(os.path.join(directory, '*.sarif'))):
        match = SARIF_NAME.match(os.path.basename(path))
        if not match:
            continue
        sarif = load_json(path)
        if not isinstance(sarif, dict) or not sarif.get('runs'):
            print(f'{relative(path)} holds no analysis run; the compiler did not report.', file=sys.stderr)
            sys.exit(2)
        logs.append((match['project'], match['tfm'], sarif))
    found = {(project, tfm) for project, tfm, _ in logs}
    if found != expected or len(logs) != len(expected):
        missing = ', '.join(f'{project}.{tfm}' for project, tfm in sorted(expected - found)) or 'none'
        unexpected = ', '.join(f'{project}.{tfm}' for project, tfm in sorted(found - expected)) or 'none'
        print(f'Expected {len(expected)} SARIF logs in {directory} but found {len(logs)}; missing: {missing}; '
              f'unexpected: {unexpected}. The analysis did not run for every project and target framework.',
              file=sys.stderr)
        sys.exit(2)
    return logs


def suppressions():
    """Trim/AOT suppressions under Source/DotNET and in root configuration, counted per repository-relative file."""
    found = Counter()
    for name in ROOT_CONFIGURATION:
        path = os.path.join(ROOT, name)
        if os.path.isfile(path):
            with open(path, encoding='utf-8', errors='replace') as handle:
                count = len(SUPPRESSION_IN_CONFIGURATION.findall(handle.read()))
            if count:
                found[relative(path)] = count
    for directory, directories, files in os.walk(SOURCE):
        directories[:] = sorted(d for d in directories if d not in SKIPPED_DIRECTORIES)
        for name in sorted(files):
            if name.endswith('.cs'):
                pattern = SUPPRESSION_IN_CODE
            elif name.endswith(CONFIGURATION_FILES):
                pattern = SUPPRESSION_IN_CONFIGURATION
            else:
                continue
            path = os.path.join(directory, name)
            with open(path, encoding='utf-8', errors='replace') as handle:
                count = len(pattern.findall(handle.read()))
            if count:
                found[relative(path)] = count
    return dict(found)


def compare(kind, baseline, actual):
    """Differences between two {key: count} maps as (direction, key, text); empty when they match."""
    problems = []
    for key in sorted(set(baseline) | set(actual)):
        expected, got = baseline.get(key, 0), actual.get(key, 0)
        if got > expected:
            label = 'new' if expected == 0 else 'increased'
            problems.append(('added', key, f'  {label} {kind}: {key}  (baseline {expected}, now {got})'))
        elif got < expected:
            label = 'gone' if got == 0 else 'decreased'
            problems.append(('removed', key, f'  {label} {kind}: {key}  (baseline {expected}, now {got})'))
    return problems


def sdk_version():
    try:
        return subprocess.run(['dotnet', '--version'], cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL,
                              text=True, check=True).stdout.strip()
    except (OSError, subprocess.CalledProcessError):
        return 'unknown'


def document(diagnostics, suppressed):
    by_code = Counter()
    by_tfm = Counter()
    for key, count in diagnostics.items():
        code, _, tfm = key.split('|')
        by_code[code] += count
        by_tfm[tfm] += count
    return {
        '_comment': 'Generated by `python3 scripts/aot-ratchet.py --update`; do not edit by hand. See #2859.',
        'sdk': sdk_version(),
        'totals': {
            'diagnostics': sum(diagnostics.values()),
            'keys': len(diagnostics),
            'byCode': dict(sorted(by_code.items())),
            'byTargetFramework': dict(sorted(by_tfm.items())),
            'suppressions': sum(suppressed.values()),
        },
        'diagnostics': dict(sorted(diagnostics.items())),
        'suppressions': dict(sorted(suppressed.items())),
    }


def solution_filter(projects, directory):
    """A solution filter, written to directory, of the covered projects: one build covers all of them and what they
    reference. Its solution path is relative to the filter, as the format requires."""
    solution = os.path.join(ROOT, next(name for name in sorted(os.listdir(ROOT)) if name.endswith('.slnx')))
    path = os.path.join(directory, 'aot-ratchet.slnf')
    document = {'solution': {'path': os.path.relpath(solution, directory).replace(os.sep, '\\'),
                             'projects': [project.replace('/', '\\') for project in projects]}}
    with open(path, 'w', encoding='utf-8') as handle:
        json.dump(document, handle, indent=2)
    return path


def build(directory, projects):
    """Run one analysis build of all covered projects, writing its SARIF logs to directory; exit 2 when it fails."""
    for stale in glob.glob(os.path.join(directory, '*.sarif')):
        os.remove(stale)
    with tempfile.TemporaryDirectory(prefix='aot-ratchet-filter-') as temporary:
        run_build(solution_filter(projects, temporary), directory)


def run_build(target, directory):
    command = [
        'dotnet', 'build', target,
        '--configuration', 'Release',
        # Analyzers only report when the compiler runs, so an up-to-date project would report nothing.
        '--no-incremental',
        '-p:CratisAotAnalysis=true',
        f'-p:CratisAotAnalysisOutput={directory}',
        '-tl:off', '-nologo', '-clp:NoSummary', '-v:minimal',
    ]
    print('Running: ' + ' '.join(command), flush=True)
    result = subprocess.run(command, cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
    if result.returncode != 0:
        print(result.stdout)
        print(f'The analysis build failed with exit code {result.returncode}.', file=sys.stderr)
        sys.exit(2)


def check(diagnostics, examples, suppressed, baseline):
    problems = (compare('diagnostic', baseline.get('diagnostics', {}), diagnostics)
                + compare('suppression', baseline.get('suppressions', {}), suppressed))
    if not problems:
        print(f'Trim/AOT ratchet passed: {sum(diagnostics.values())} diagnostics in {len(diagnostics)} keys and '
              f'{sum(suppressed.values())} suppressions, matching {relative(BASELINE)}.')
        return 0

    added = [problem for problem in problems if problem[0] == 'added']
    removed = [problem for problem in problems if problem[0] == 'removed']
    if added:
        print('Trim/AOT ratchet FAILED: this change adds trim/AOT analyzer diagnostics or suppressions.')
        print('Replace the unsafe path (generated metadata, a JsonSerializerContext, annotated types) rather than '
              'suppressing it; see #2204.\n')
        for _, key, text in added:
            print(text)
            for example in examples.get(key, [])[:5]:
                print(f'      {example}')
        print()
    if removed:
        print('Trim/AOT ratchet FAILED: the baseline lists diagnostics or suppressions this build no longer '
              'produces.')
        print('If this change fixed them, lock the improvement in by shrinking the baseline in the same pull '
              'request.\n')
        for _, _, text in removed:
            print(text)
        print()
    current = sdk_version()
    if baseline.get('sdk') and baseline['sdk'] != current:
        print(f'Note: the baseline was made with SDK {baseline["sdk"]} and this run used {current}. The trim analyzer '
              'ships with the SDK, so a difference nobody caused may come from the SDK version.\n')
    print('A diagnostic that disappears because a member gained [RequiresUnreferencedCode] or [RequiresDynamicCode] '
          'moves the warning to its callers; check that is intended before shrinking the baseline.')
    print('When the difference is intended, run `python3 scripts/aot-ratchet.py --update` and commit '
          f'{relative(BASELINE)}. A baseline that grows needs a reviewer to accept why.')
    return 1


def self_test():
    """Exercise the parsing and comparison on synthetic input, without building."""
    def result(code, line, suppressed=False):
        value = {
            'ruleId': code,
            'message': {'text': 'm'},
            'locations': [{'physicalLocation': {
                'artifactLocation': {'uri': 'file://' + os.path.join(ROOT, 'Source/DotNET/Arc.Core/A%20B.cs')},
                'region': {'startLine': line, 'startColumn': 1}}}],
        }
        if suppressed:
            value['suppressions'] = [{'kind': 'inSource'}]
        return value
    sarif = {'runs': [{'results': [result('IL2026', 1), result('IL2026', 2), result('CA1000', 3),
                                   result('IL3050', 4, suppressed=True)]}]}
    counts, _ = collect([('Arc.Core', 'net10.0', sarif), ('Arc.Core', 'net8.0', sarif)])
    key = 'IL2026|Source/DotNET/Arc.Core/A B.cs|net10.0'
    assert counts == {key: 2, 'IL2026|Source/DotNET/Arc.Core/A B.cs|net8.0': 2}, counts
    assert SARIF_NAME.match('Arc.Core.net10.0.sarif')['project'] == 'Arc.Core'
    assert SARIF_NAME.match('Arc.net8.0.sarif')['tfm'] == 'net8.0'

    assert not compare('diagnostic', {key: 2}, {key: 2})
    assert [p[0] for p in compare('diagnostic', {key: 1}, {key: 2})] == ['added']
    assert [p[0] for p in compare('diagnostic', {}, {key: 1})] == ['added']
    assert [p[0] for p in compare('diagnostic', {key: 2}, {key: 1})] == ['removed']
    assert [p[0] for p in compare('diagnostic', {key: 2}, {})] == ['removed']

    for code in ('[UnconditionalSuppressMessage("x", "y")]', '[SuppressMessage("Trimming", "IL2026")]',
                 '[SuppressMessage( "AOT", "IL3050")]', '#pragma warning disable CA1000, IL2075',
                 '[UnconditionalSuppressMessage("Trimming", "IL2026")]',
                 '[SuppressMessageAttribute("Trimming", "IL2026")]', '[SuppressMessage(category: "AOT", "IL3050")]',
                 '#pragma warning disable', '#pragma warning disable // everything'):
        assert len(SUPPRESSION_IN_CODE.findall(code)) == 1, code
    for code in ('[SuppressMessage("Design", "CA1000")]', '#pragma warning disable CA1000',
                 '#pragma warning restore'):
        assert not SUPPRESSION_IN_CODE.findall(code), code
    assert SUPPRESSION_IN_CONFIGURATION.findall('<NoWarn>$(NoWarn);IL2026</NoWarn>')
    assert SUPPRESSION_IN_CONFIGURATION.findall('dotnet_diagnostic.IL3050.severity = none')
    assert not SUPPRESSION_IN_CONFIGURATION.findall('<NoWarn>CA1000</NoWarn>')
    assert analyzed_frameworks('net8.0;net9.0;net10.0') == ['net8.0', 'net9.0', 'net10.0']
    assert analyzed_frameworks('netstandard2.0;net7.0; net10.0 ;net9.0-windows') == ['net10.0', 'net9.0-windows']
    assert analyzed_frameworks('netstandard2.0') == []
    expected = expected_logs({'Source/DotNET/Arc.Core/Arc.Core.csproj': ['net8.0', 'net10.0'],
                              'Source/DotNET/MongoDB/MongoDB.csproj': ['net8.0', 'net10.0']})
    assert expected == {('Arc.Core', 'net8.0'), ('Arc.Core', 'net10.0'),
                        ('MongoDB', 'net8.0'), ('MongoDB', 'net10.0')}, expected
    with tempfile.TemporaryDirectory() as directory:
        def log(name):
            with open(os.path.join(directory, name), 'w', encoding='utf-8') as handle:
                json.dump({'runs': [{'results': []}]}, handle)
        for name in ('Arc.Core.net8.0.sarif', 'Arc.Core.net10.0.sarif', 'MongoDB.net8.0.sarif',
                     'MongoDB.net10.0.sarif'):
            log(name)
        assert len(read_logs(directory, expected)) == 4
        # A missing log, an unexpected extra one and a log of the wrong project must each fail the run.
        for wrong in (expected | {('Chronicle', 'net8.0')}, expected - {('MongoDB', 'net10.0')},
                      (expected - {('MongoDB', 'net10.0')}) | {('Chronicle', 'net10.0')}):
            try:
                with contextlib.redirect_stderr(io.StringIO()):
                    read_logs(directory, wrong)
            except SystemExit as failure:
                assert failure.code == 2, failure.code
            else:
                raise AssertionError(f'read_logs accepted {sorted(wrong)}')
        filter_path = solution_filter(['Source/DotNET/Arc/Arc.csproj'], directory)
        with open(filter_path, encoding='utf-8') as handle:
            filtered = json.load(handle)['solution']
        assert filtered['projects'] == ['Source\\DotNET\\Arc\\Arc.csproj'], filtered
        assert os.path.isfile(os.path.normpath(os.path.join(directory, filtered['path'].replace('\\', os.sep))))
    projects = covered_projects()
    assert 'Source/DotNET/Arc.Core/Arc.Core.csproj' in projects and 'Source/DotNET/Arc/Arc.csproj' in projects, projects
    windows = {'locations': [{'physicalLocation': {'artifactLocation': {'uri': 'file:///C:/repo/A.cs'}}}]}
    assert file_of(windows, '') in ('C:/repo/A.cs', 'C:\\repo\\A.cs'), file_of(windows, '')
    print('Self-test passed.')
    return 0


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--update', action='store_true', help='rewrite the baseline from this build')
    parser.add_argument('--sarif', metavar='DIR', help='keep the SARIF logs in DIR (default: a temporary directory)')
    parser.add_argument('--no-build', action='store_true', help='read the SARIF logs already in --sarif DIR')
    parser.add_argument('--self-test', action='store_true', help='check the comparison logic, then exit')
    arguments = parser.parse_args()

    if arguments.self_test:
        return self_test()
    if arguments.no_build and not arguments.sarif:
        parser.error('--no-build needs --sarif DIR')

    with tempfile.TemporaryDirectory(prefix='aot-ratchet-') as temporary:
        directory = os.path.abspath(arguments.sarif) if arguments.sarif else temporary
        if re.search(r'[\s,;]', directory):
            # MSBuild's ErrorLog value is `path,version=2.1`; a space, comma or semicolon breaks it.
            parser.error(f'--sarif DIR must not contain spaces, commas or semicolons: {directory}')
        os.makedirs(directory, exist_ok=True)
        projects = covered_projects()
        if not projects:
            print(f'No project under {relative(SOURCE)} imports {PROPS_IMPORT}; nothing to analyze.', file=sys.stderr)
            return 2
        frameworks = {project: project_frameworks(project) for project in projects}
        if not arguments.no_build:
            build(directory, projects)
        diagnostics, examples = collect(read_logs(directory, expected_logs(frameworks)))
    suppressed = suppressions()

    if arguments.update:
        with open(BASELINE, 'w', encoding='utf-8') as handle:
            json.dump(document(diagnostics, suppressed), handle, indent=2)
            handle.write('\n')
        print(f'Wrote {relative(BASELINE)}: {sum(diagnostics.values())} diagnostics in {len(diagnostics)} keys and '
              f'{sum(suppressed.values())} suppressions.')
        return 0

    if not os.path.exists(BASELINE):
        print(f'No baseline at {relative(BASELINE)}; run with --update to create it.', file=sys.stderr)
        return 2
    baseline = load_json(BASELINE)
    if not isinstance(baseline, dict):
        print(f'{relative(BASELINE)} is not a JSON object; regenerate it with --update.', file=sys.stderr)
        return 2
    return check(diagnostics, examples, suppressed, baseline)


if __name__ == '__main__':
    sys.exit(main())
