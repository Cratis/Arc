#!/usr/bin/env python3
"""Compose the pull request body for a Chronicle dependency update.

The body doubles as Arc's release notes - `cratis/release-action` publishes the merged
pull request's body verbatim as the GitHub release - so it has to follow
`.github/pull_request_template.md` exactly: an optional short summary plus the
`Added`/`Changed`/`Fixed`/`Removed`/`Security`/`Deprecated` sections that apply, and
nothing else. Anything about CI belongs in a pull request comment, never in the body.

Chronicle's own release notes follow the same template, so every Chronicle release picked
up by the update contributes its bullets to the matching section here, attributed to the
release it came from. Issue references are rewritten to point at Chronicle's issue tracker
rather than resolving against this repository.
"""

import argparse
import json
import re
import subprocess
import sys

SECTIONS = ['Added', 'Changed', 'Fixed', 'Removed', 'Security', 'Deprecated']
PACKAGES = '`Cratis.Chronicle`, `Cratis.Chronicle.AspNetCore` and `Cratis.Chronicle.Testing`'

HEADING = re.compile(r'^#{1,6}\s+(.*?)\s*#*\s*$')
TOP_LEVEL_BULLET = re.compile(r'^[-*]\s+')
ISSUE_REFERENCE = re.compile(r'(?<![\w/#])#(\d+)\b')


def version_key(version):
    """Sort key for a semantic version, ordering a prerelease before its release."""
    core, _, prerelease = version.lstrip('v').partition('-')
    parts = [int(piece) if piece.isdigit() else 0 for piece in core.split('.')]
    parts = (parts + [0, 0, 0])[:3]
    return (parts[0], parts[1], parts[2], 0 if prerelease else 1, prerelease)


def fetch_releases(repository):
    """All published releases of the repository, newest first. Empty when they can't be read."""
    try:
        result = subprocess.run(
            ['gh', 'api', '-H', 'Accept: application/vnd.github+json',
             f'repos/{repository}/releases?per_page=100'],
            capture_output=True, text=True, timeout=120)
    except (OSError, subprocess.SubprocessError) as error:
        print(f'Could not reach the GitHub API for {repository}: {error}', file=sys.stderr)
        return []

    if result.returncode != 0:
        print(f'Could not read releases for {repository}: {result.stderr.strip()}', file=sys.stderr)
        return []

    return json.loads(result.stdout)


def releases_in_range(releases, previous, target):
    """The published releases that the update picks up - everything after previous, up to target.

    Without a known previous version there is no lower bound to work from, so the range
    narrows to the target release alone rather than folding in the entire history.
    """
    target_key = version_key(target)
    previous_key = version_key(previous) if previous else None

    picked = []
    for release in releases:
        if release.get('draft') or release.get('prerelease'):
            continue
        version = (release.get('tag_name') or '').lstrip('v')
        if not version:
            continue
        key = version_key(version)
        if key > target_key:
            continue
        if previous_key is None:
            if key != target_key:
                continue
        elif key <= previous_key:
            continue
        picked.append((version, release))

    picked.sort(key=lambda entry: version_key(entry[0]))
    return picked


def parse_sections(body):
    """The template sections of a release body, each as a list of top-level bullets."""
    sections = {name: [] for name in SECTIONS}
    section = None
    bullet = None

    def flush():
        nonlocal bullet
        if section is not None and bullet:
            sections[section].append(bullet)
        bullet = None

    for line in (body or '').replace('\r\n', '\n').split('\n'):
        heading = HEADING.match(line)
        if heading:
            flush()
            title = heading.group(1).strip()
            section = title if title in sections else None
            continue

        if section is None:
            continue

        if TOP_LEVEL_BULLET.match(line):
            flush()
            bullet = [line.rstrip()]
        elif bullet is not None and line.strip():
            # A wrapped line or a nested bullet - it belongs to the bullet above it.
            bullet.append(line.rstrip())
        else:
            flush()

    flush()
    return sections


def render_bullet(lines, version, repository):
    """One inherited bullet, attributed to the Chronicle release it came from."""
    text = TOP_LEVEL_BULLET.sub('', lines[0], count=1)
    rendered = '\n'.join([f'- Chronicle {version}: {text}'] + lines[1:])
    return ISSUE_REFERENCE.sub(rf'{repository}#\1', rendered)


def compose_summary(previous, target, picked):
    """The summary line - the cohesive theme of the update, with links to dig into."""
    if previous and previous != target:
        sentence = f'{PACKAGES} are updated from `{previous}` to `{target}`'
    else:
        sentence = f'{PACKAGES} are updated to `{target}`'

    links = [f'[{version}]({release.get("html_url")})'
             for version, release in picked if release.get('html_url')]

    if not links:
        return f'{sentence}.'
    if len(links) == 1:
        return f'{sentence}, picking up {links[0]}.'
    return f'{sentence}, picking up {", ".join(links[:-1])} and {links[-1]}.'


def compose(previous, target, repository, picked):
    """The full pull request body, in pull_request_template.md shape."""
    inherited = {name: [] for name in SECTIONS}
    for version, release in picked:
        for name, bullets in parse_sections(release.get('body')).items():
            inherited[name].extend(render_bullet(bullet, version, repository) for bullet in bullets)

    changed = [f'- {PACKAGES} are updated to `{target}`'] + inherited['Changed']

    # Without the Chronicle releases to point at, the summary would only restate the single
    # bullet below it - the template says to drop it in exactly that case.
    body = ['# Summary', '', compose_summary(previous, target, picked)] if picked else []

    for name in SECTIONS:
        bullets = changed if name == 'Changed' else inherited[name]
        if bullets:
            body += ['', f'## {name}', ''] + bullets

    return '\n'.join(body).strip() + '\n'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repository', default='cratis/chronicle', help='Repository the release notes are read from.')
    parser.add_argument('--previous', default='', help='Chronicle version currently pinned. Empty when unknown.')
    parser.add_argument('--target', required=True, help='Chronicle version being updated to.')
    parser.add_argument('--output', help='File to write the body to. Defaults to standard output.')
    arguments = parser.parse_args()

    previous = arguments.previous.strip().lstrip('v')
    target = arguments.target.strip().lstrip('v')

    picked = releases_in_range(fetch_releases(arguments.repository), previous, target)
    if not picked:
        print(f'No {arguments.repository} release notes were found for the update - '
              'falling back to the bare version bump.', file=sys.stderr)
    else:
        print(f'Folding in {len(picked)} {arguments.repository} release(s): '
              f'{", ".join(version for version, _ in picked)}.', file=sys.stderr)

    body = compose(previous, target, arguments.repository, picked)

    if arguments.output:
        with open(arguments.output, 'w', encoding='utf-8') as file:
            file.write(body)
    else:
        sys.stdout.write(body)


if __name__ == '__main__':
    main()
