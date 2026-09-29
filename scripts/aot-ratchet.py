#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
"""Ratchet for the trim and NativeAOT analyzer diagnostics of Arc.Core and Arc (#2859, part of #2204).

Arc is not trim or AOT compatible yet, so failing on every diagnostic would block every pull request. Instead
this builds the two projects with the analyzers switched on (CratisAotAnalysis=true, honored only by the
projects importing Source/DotNET/AotAnalysis.props) and holds what they report to the checked-in baseline in
Source/DotNET/aot-baseline.json:

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
import glob
import json
import os
import re
import subprocess
import sys
import tempfile
from collections import Counter
from urllib.parse import unquote, urlparse

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BASELINE = os.path.join(ROOT, 'Source', 'DotNET', 'aot-baseline.json')
SOURCE = os.path.join(ROOT, 'Source', 'DotNET')

# Arc.csproj references Arc.Core, so building it builds and analyzes both.
PROJECT = os.path.join('Source', 'DotNET', 'Arc', 'Arc.csproj')
EXPECTED_LOGS = 6  # Arc.Core and Arc, each for net8.0, net9.0 and net10.0.

SUPPRESSION_IN_CODE = re.compile(
    r'UnconditionalSuppressMessage'
    r'|(?<!Unconditional)SuppressMessage\s*\(\s*"(?:Trimming|AOT|SingleFile)"'
    r'|#\s*pragma\s+warning\s+disable\b[^\n]*\bIL\d{4}')
SUPPRESSION_IN_CONFIGURATION = re.compile(
    r'<NoWarn>[^<]*\bIL\d{4}|dotnet_diagnostic\.IL\d{4}\.severity')
CONFIGURATION_FILES = ('.csproj', '.props', '.targets', '.editorconfig', '.globalconfig')
SKIPPED_DIRECTORIES = {'bin', 'obj', 'node_modules', '.git'}
SARIF_NAME = re.compile(r'^(?P<project>.+)\.(?P<tfm>net[^.]*(?:\.\d+)?)\.sarif$')


def relative(path):
    """Repository-relative path with forward slashes; the path as reported when it is outside the repository."""
    normalized = os.path.normpath(path.strip())
    if os.path.isabs(normalized):
        inside = os.path.relpath(normalized, ROOT)
        if not inside.startswith('..'):
            normalized = inside
    return normalized.replace(os.sep, '/')


def file_of(result, fallback):
    for location in result.get('locations') or []:
        uri = location.get('physicalLocation', {}).get('artifactLocation', {}).get('uri')
        if uri:
            parsed = urlparse(uri)
            return relative(unquote(parsed.path) if parsed.scheme == 'file' else unquote(uri))
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


def read_logs(directory):
    logs = []
    for path in sorted(glob.glob(os.path.join(directory, '*.sarif'))):
        match = SARIF_NAME.match(os.path.basename(path))
        if not match:
            continue
        with open(path, encoding='utf-8-sig') as handle:
            logs.append((match['project'], match['tfm'], json.load(handle)))
    if len(logs) != EXPECTED_LOGS:
        names = ', '.join(f'{project}.{tfm}' for project, tfm, _ in logs) or 'none'
        print(f'Expected {EXPECTED_LOGS} SARIF logs in {directory} but found {len(logs)} ({names}); the analysis '
              'did not run for every project and target framework.', file=sys.stderr)
        sys.exit(2)
    return logs


def suppressions():
    """Trim/AOT suppressions under Source/DotNET, counted per repository-relative file."""
    found = Counter()
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


def document(diagnostics, suppressed):
    by_code = Counter()
    by_tfm = Counter()
    for key, count in diagnostics.items():
        code, _, tfm = key.split('|')
        by_code[code] += count
        by_tfm[tfm] += count
    return {
        '_comment': 'Generated by `python3 scripts/aot-ratchet.py --update`; do not edit by hand. See #2859.',
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


def build(directory):
    """Run the analysis build, writing its SARIF logs to directory; exit 2 when it fails."""
    for stale in glob.glob(os.path.join(directory, '*.sarif')):
        os.remove(stale)
    command = [
        'dotnet', 'build', PROJECT,
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
                 '[UnconditionalSuppressMessage("Trimming", "IL2026")]'):
        assert len(SUPPRESSION_IN_CODE.findall(code)) == 1, code
    for code in ('[SuppressMessage("Design", "CA1000")]', '#pragma warning disable CA1000'):
        assert not SUPPRESSION_IN_CODE.findall(code), code
    assert SUPPRESSION_IN_CONFIGURATION.findall('<NoWarn>$(NoWarn);IL2026</NoWarn>')
    assert SUPPRESSION_IN_CONFIGURATION.findall('dotnet_diagnostic.IL3050.severity = none')
    assert not SUPPRESSION_IN_CONFIGURATION.findall('<NoWarn>CA1000</NoWarn>')
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
        os.makedirs(directory, exist_ok=True)
        if not arguments.no_build:
            build(directory)
        diagnostics, examples = collect(read_logs(directory))
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
    with open(BASELINE, encoding='utf-8') as handle:
        baseline = json.load(handle)
    return check(diagnostics, examples, suppressed, baseline)


if __name__ == '__main__':
    sys.exit(main())
