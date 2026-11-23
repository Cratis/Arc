#!/bin/bash

# Link Verification Script
# Verifies the links in the documentation of this repository, and is used both
# by verify-markdown.sh and by the Markdown Verification workflow so the two
# cannot drift apart.
#
# What is verified here:
#   - relative links between pages in this repository
#   - external http(s) links
#
# What is not verified here:
#   Links written as site-absolute paths (/arc/..., /chronicle/...) address the
#   aggregated documentation site, where each product's Documentation folder is
#   mounted under its own prefix. They cannot resolve against a single
#   repository and are verified when that site is built. They are skipped by
#   rule rather than by name, so a new prefix needs no change here.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

# Pinned so that a new major version cannot silently change what is scanned.
LINKINATOR_VERSION="8.0.2"

# linkinator serves the scanned files from a local web server, so every internal
# link resolves to http://127.0.0.1:<port>/<path>. Skipping what falls outside
# /Documentation/ therefore skips exactly the site-absolute paths this
# repository cannot resolve, and nothing else. The pattern must never match the
# crawl root itself - a pattern that did is what turned this check into a no-op.
SITE_ABSOLUTE_LINKS='^https?://(localhost|127\.0\.0\.1):[0-9]+/(?!Documentation/)'

cd "$ROOT_DIR"

if ! command -v npx &> /dev/null; then
    echo "Error: npx is not installed. Please install Node.js and npm."
    exit 1
fi

echo "Checking links in Documentation..."
echo "This may take a few minutes to check all links..."
echo ""

REPORT=$(mktemp)
LOG=$(mktemp)
trap 'rm -f "$REPORT" "$LOG"' EXIT

set +e
# One glob covering both extensions rather than one per extension: linkinator
# aborts on any glob that matches nothing, so a separate .mdx glob would fail a
# repository whose pages are all .md while every link in it is fine.
#
# Limit requests to 10 concurrent connections so shared hosts such as GitHub are
# not hit in a burst. Retry 429 responses that carry a retry-after header, and
# retry transient 5xx and network failures three times; linkinator's exponential
# backoff plus 1000ms of random jitter gives an affected request up to four
# attempts.
#
# The report is written as JSON and judged by verify-links-results.mjs, which
# counts what was scanned and treats a github.com 429/5xx that survives the
# retries as a warning. linkinator's own exit code is not the verdict: it would
# fail the run on GitHub throttling a burst of requests.
NO_COLOR=1 npx --yes "linkinator@$LINKINATOR_VERSION" \
    "Documentation/**/*.{md,mdx}" \
    --markdown \
    --recurse \
    --directory-listing \
    --concurrency 10 \
    --retry \
    --retry-errors \
    --retry-errors-count 3 \
    --retry-errors-jitter 1000 \
    --status-code "403:ok" \
    --skip "$SITE_ABSOLUTE_LINKS" \
    --format json > "$REPORT" 2> "$LOG"
LINKINATOR_EXIT_CODE=$?
set -e

# linkinator exits 1 when a link is broken; anything else means it did not run
# to completion, and its diagnostics are what explains why.
if [ $LINKINATOR_EXIT_CODE -ne 0 ] && [ $LINKINATOR_EXIT_CODE -ne 1 ]; then
    cat "$LOG"
    echo "✗ Link verification could not run - linkinator exited with $LINKINATOR_EXIT_CODE."
    exit $LINKINATOR_EXIT_CODE
fi

if ! node "$SCRIPT_DIR/verify-links-results.mjs" "$REPORT"; then
    grep -v '^npm notice' "$LOG" || true
    exit 1
fi
echo "  Site-absolute links (/arc/..., /chronicle/...) resolve only on the aggregated"
echo "  documentation site and are verified when that site is built."
