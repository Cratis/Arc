#!/bin/bash
# Check if a JavaScript package dist is up to date
# Usage: check-js-timestamp.sh <package-path>
# Exit code 0 = up to date, 1 = needs rebuild

if [ ! -f "$1/dist/esm/index.d.ts" ]; then
  exit 1
fi

result=$(find "$1" -not -path "*/dist/*" -type f \( -name "*.ts" -o -name "*.tsx" -o -name "package.json" -o -name "tsconfig.json" -o -name "rollup.config.mjs" \) -newer "$1/dist/esm/index.d.ts" -print -quit)
[ -z "$result" ] && exit 0 || exit 1
