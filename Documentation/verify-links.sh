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

set +e
OUTPUT=$(npx --yes "linkinator@$LINKINATOR_VERSION" \
    "Documentation/**/*.md" \
    "Documentation/**/*.mdx" \
    --markdown \
    --recurse \
    --directory-listing \
    --verbosity error \
    --status-code "403:ok" \
    --skip "$SITE_ABSOLUTE_LINKS" 2>&1)
LINKINATOR_EXIT_CODE=$?
set -e

echo "$OUTPUT"

# How many links were actually looked at. A check that scans nothing reports
# success while verifying nothing, which reads exactly like a passing check.
SCANNED=$(echo "$OUTPUT" | grep -oE 'canned [0-9]+ link' | grep -oE '[0-9]+' | tail -1)

echo ""

if [ -z "$SCANNED" ]; then
    echo "✗ Link verification could not determine how many links were scanned."
    echo "  linkinator reported no scan summary - the file globs most likely matched nothing."
    exit 1
fi

if [ "$SCANNED" -eq 0 ]; then
    echo "✗ Link verification scanned zero links."
    echo "  Nothing was checked, so this run proves nothing about the links in Documentation."
    exit 1
fi

if [ $LINKINATOR_EXIT_CODE -ne 0 ]; then
    echo "✗ Link verification failed - $SCANNED links scanned."
    exit $LINKINATOR_EXIT_CODE
fi

echo "✓ Link verification passed - $SCANNED links scanned."
echo "  Site-absolute links (/arc/..., /chronicle/...) resolve only on the aggregated"
echo "  documentation site and are verified when that site is built."
