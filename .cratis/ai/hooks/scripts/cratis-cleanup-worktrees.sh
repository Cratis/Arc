#!/usr/bin/env bash
# cratis-ai-managed: hooks/scripts/cratis-cleanup-worktrees.sh
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Deterministic cleanup of delivered work: linked worktrees and local branches whose work has
# reached the default branch. One script, every harness: Claude Code and Pi run it from hooks, and
# the ship-changes prompt runs it as a mandatory last step for harnesses without hooks.
#
# A worktree or branch is removed only on EVIDENCE that its work was delivered, never on a guess:
#   1. its tip is merged into origin/<default> AND was integrated (it is not simply sitting on the
#      default branch's first-parent history, which is where a brand-new, empty branch sits), or
#   2. its tip is merged into origin/<default> and its upstream is gone (the PR branch was deleted), or
#   3. GitHub reports a merged pull request whose head commit is exactly this tip.
# Anything else is KEPT and reported with the reason. Nothing is ever forced: a dirty, locked,
# current or live-held worktree is kept. Ignored/untracked files must be archived or removed first,
# including .ai-work/. A non-empty .ai-work/ is also preserved when tracked, unless opted in below.
# Holder checks prefer validated pi-holders --json with jq, otherwise per-process lsof names (never
# +D): one process-wide listing per candidate, up to three attempts on errors/incomplete results.
# Only this script and its descendants are excluded; ancestor sessions still count as holders.
# Missing, failed, unparseable or ambiguous checks keep the worktree (including shared-inode names).
# `git worktree remove` is never given --force.
#
# Usage:
#   cratis-cleanup-worktrees.sh [--dry-run] [--quiet] [--no-fetch] [--include-work-records=<path>]...
#                                                                 run in the repository (any worktree)
#   --include-work-records=<path>                                bypass record preservation for this
#                                                                 exact worktree only; repeatable, manual
#                                                                 only, ignored in hook mode. It permits
#                                                                 clean tracked records, never ignored or
#                                                                 untracked files. Bare flag exits 2.
#   cratis-cleanup-worktrees.sh --hook                                Claude hook mode: reads the hook JSON
#                                                                     on stdin, acts only when relevant
#   cratis-cleanup-worktrees.sh --self-test                           plants cases and fails if any is wrong
#
# Exit: 0 ran (kept items are not failures), 2 could not run. Opt out: CRATIS_HOOKS_SKIP_CLEANUP=1.
# Portable: bash 3.2, BSD + GNU userland.
# shellcheck disable=SC2016  # the self-test checks are single-quoted on purpose: they are eval-ed later
set -uo pipefail

DRY=0; QUIET=0; FETCH=1; HOOK=0; SELFTEST=0; WORK_RECORD_PATHS=()
for a in "$@"; do
    case "$a" in
        --dry-run) DRY=1 ;; --quiet) QUIET=1 ;; --no-fetch) FETCH=0 ;;
        --hook) HOOK=1; QUIET=1 ;; --self-test) SELFTEST=1 ;;
        --include-work-records|--include-work-records=)
            echo "cratis-cleanup-worktrees: --include-work-records requires an explicit worktree path (--include-work-records=<path>)" >&2; exit 2 ;;
        --include-work-records=*)
            record_path=${a#*=}
            record_path=$(cd "$record_path" 2>/dev/null && pwd -P) || { echo "cratis-cleanup-worktrees: cannot resolve worktree path '${a#*=}'" >&2; exit 2; }
            WORK_RECORD_PATHS+=("$record_path") ;;
        *) echo "cratis-cleanup-worktrees: unknown argument '$a'" >&2; exit 2 ;;
    esac
done

# ── Hook mode: decide whether this event is worth a sweep ─────────────────────
EVENT="PostToolUse"
if [ "$HOOK" = 1 ]; then
    WORK_RECORD_PATHS=()
    payload=''; IFS= read -r -d '' payload || true
    [ "${CRATIS_HOOKS_SKIP_CLEANUP:-}" = 1 ] && exit 0
    case "$payload" in *'"SessionStart"'*) EVENT="SessionStart" ;; esac
    if [ "$EVENT" = "PostToolUse" ]; then
        flat=${payload//\\n/ }; flat=${flat//\\\"/\"}
        # Only commands that deliver or integrate work; everything else costs nothing.
        if [[ ! "$flat" =~ (gh[[:space:]]+pr[[:space:]]+merge|git[[:space:]]+(-C[[:space:]]+[^[:space:]]+[[:space:]]+)?(pull|merge|worktree[[:space:]]+(add|remove|prune))|git[[:space:]]+push[[:space:]].*--delete) ]]; then
            exit 0
        fi
    fi
    cwd=""
    command -v jq >/dev/null 2>&1 && cwd=$(printf '%s' "$payload" | jq -r '.cwd // empty' 2>/dev/null)
    if [ -n "$cwd" ] && [ -d "$cwd" ]; then cd "$cwd" || exit 0; fi
fi
[ "${CRATIS_HOOKS_SKIP_CLEANUP:-}" = 1 ] && [ "$SELFTEST" = 0 ] && exit 0

# ── Self-test ─────────────────────────────────────────────────────────────────
if [ "$SELFTEST" = 1 ]; then
    self=$(cd "$(dirname "$0")" && pwd)/$(basename "$0")
    t=$(mktemp -d "${TMPDIR:-/tmp}/cleanup-selftest.XXXXXX"); holder=''
    trap '[ -z "$holder" ] || { kill "$holder" 2>/dev/null; wait "$holder" 2>/dev/null; }; rm -rf "$t"' EXIT
    t=$(cd "$t" && pwd -P)
    export GIT_AUTHOR_NAME=t GIT_AUTHOR_EMAIL=t@t GIT_COMMITTER_NAME=t GIT_COMMITTER_EMAIL=t@t CRATIS_CLEANUP_NO_GH=1
    git init -q --bare -b main "$t/origin.git"
    git clone -q "$t/origin.git" "$t/repo" 2>/dev/null; cd "$t/repo" || exit 2
    git config core.excludesFile /dev/null
    git checkout -q -b main 2>/dev/null; echo a > a; git add a; git commit -qm base; git push -q origin main
    base=$(git rev-parse main)
    mk() { git worktree add -q -b "$1" "$t/wt-$1" "$base"; }
    mk delivered;  (cd "$t/wt-delivered" && echo d > d && git add d && git commit -qm d)
    mk dirty;      (cd "$t/wt-dirty" && echo x > x && git add x && git commit -qm x)
    mk unmerged;   (cd "$t/wt-unmerged" && echo u > u && git add u && git commit -qm u)
    mk fresh                                    # brand new, no commits: must survive
    for b in records empty-records locked locked-detached tracked-records scoped-records ignored live-holder; do
        mk "$b"; (cd "$t/wt-$b" && echo "$b" > "$b" && git add "$b" && git commit -qm "$b")
        case "$b" in
            tracked-records|scoped-records)
                mkdir "$t/wt-$b/.ai-work"; echo handoff > "$t/wt-$b/.ai-work/.handoff"
                git -C "$t/wt-$b" add .ai-work; git -C "$t/wt-$b" commit -qm records ;;
        esac
        git merge -q --no-ff "$b" -m "merge $b"
    done
    mkdir "$t/wt-records/.ai-work" "$t/wt-empty-records/.ai-work"
    printf '.ai-work/\nignored-output\n' >> "$t/repo/.git/info/exclude"
    echo handoff > "$t/wt-records/.ai-work/.handoff"
    echo build > "$t/wt-ignored/ignored-output"
    (cd "$t/wt-live-holder" && exec sleep 120) & holder=$!
    git -C "$t/wt-locked-detached" checkout -q --detach
    git worktree lock "$t/wt-locked"
    git worktree lock --reason pinned "$t/wt-locked-detached"
    git branch loose-delivered main; (git checkout -q loose-delivered && echo l > l && git add l && git commit -qm l && git checkout -q main)
    git merge -q --no-ff delivered -m "merge delivered" && git merge -q --no-ff dirty -m "merge dirty" && git merge -q --no-ff loose-delivered -m "merge loose"
    git push -q origin main
    echo junk > "$t/wt-dirty/untracked"          # makes the merged worktree dirty
    out=$("$self" --no-fetch 2>&1) || { echo "FAIL: exit $?"; echo "$out"; exit 1; }
    fail=0; checks=0; chk() { checks=$((checks + 1)); if ! eval "$2"; then echo "FAIL: $1"; fail=1; fi; }
    chk "delivered worktree removed"   '[ ! -d "$t/wt-delivered" ]'
    chk "delivered branch removed"     '! git rev-parse -q --verify refs/heads/delivered >/dev/null'
    chk "loose merged branch removed"  '! git rev-parse -q --verify refs/heads/loose-delivered >/dev/null'
    chk "dirty worktree kept"          '[ -d "$t/wt-dirty" ]'
    chk "unmerged worktree kept"       '[ -d "$t/wt-unmerged" ]'
    chk "unmerged branch kept"         'git rev-parse -q --verify refs/heads/unmerged >/dev/null'
    chk "fresh worktree kept"          '[ -d "$t/wt-fresh" ]'
    chk "fresh branch kept"            'git rev-parse -q --verify refs/heads/fresh >/dev/null'
    chk "main kept"                    'git rev-parse -q --verify refs/heads/main >/dev/null'
    chk "report names the removal"     'grep -q "removed worktree" <<<"$out"'
    chk "work records kept"            '[ -f "$t/wt-records/.ai-work/.handoff" ]'
    chk "work records branch kept"     'git rev-parse -q --verify refs/heads/records >/dev/null'
    chk "work records reason reported" 'grep -Fq "has work records (.ai-work/)" <<<"$out"'
    chk "empty work records removed"   '[ ! -d "$t/wt-empty-records" ]'
    chk "locked worktree kept"         '[ -d "$t/wt-locked" ]'
    chk "locked branch kept"           'git rev-parse -q --verify refs/heads/locked >/dev/null'
    chk "locked reason reported"       'grep -Fq "worktree $t/wt-locked (locked): locked" <<<"$out"'
    chk "locked detached kept"         '[ -d "$t/wt-locked-detached" ]'
    chk "locked detached reported"     'grep -Fq "worktree $t/wt-locked-detached (detached): locked" <<<"$out"'
    chk "live holder kept"             '[ -d "$t/wt-live-holder" ]'
    chk "live holder reason reported"  'grep -F "worktree $t/wt-live-holder (live-holder):" <<<"$out" | grep -E ": (live holder|live process)" >/dev/null'
    chk "ignored file kept"            '[ -f "$t/wt-ignored/ignored-output" ]'
    chk "ignored file reason"          'grep -Fq "has ignored/untracked files; archive or remove them first" <<<"$out"'
    out=$("$self" --no-fetch --dry-run 2>&1) || { echo "FAIL: dry-run exit $?"; echo "$out"; exit 1; }
    chk "dry-run reports live holder"  'grep -F "kept    worktree $t/wt-live-holder (live-holder):" <<<"$out" | grep -E ": (live holder|live process)" >/dev/null'
    out=$(printf '%s' '{"hook_event_name":"SessionStart"}' | "$self" --no-fetch "--include-work-records=$t/wt-tracked-records" --hook 2>&1) || { echo "FAIL: hook exit $?"; echo "$out"; exit 1; }
    chk "hook ignores opt-in"          '[ -f "$t/wt-tracked-records/.ai-work/.handoff" ]'
    out=$("$self" --no-fetch "--include-work-records=$t/wt-records" "--include-work-records=$t/wt-tracked-records" 2>&1) || { echo "FAIL: opt-in exit $?"; echo "$out"; exit 1; }
    chk "opt-in keeps ignored records" '[ -f "$t/wt-records/.ai-work/.handoff" ]'
    chk "opt-in removes tracked records" '[ ! -d "$t/wt-tracked-records" ]'
    chk "opt-in removes branch"        '! git rev-parse -q --verify refs/heads/tracked-records >/dev/null'
    chk "opt-in scope preserves others" '[ -f "$t/wt-scoped-records/.ai-work/.handoff" ]'
    chk "opt-in keeps locked"          '[ -d "$t/wt-locked" ]'
    chk "opt-in keeps locked detached" '[ -d "$t/wt-locked-detached" ]'
    out=$("$self" --include-work-records 2>&1); code=$?
    chk "bare opt-in rejected"         "[ '$code' = 2 ]"
    chk "bare opt-in guidance"         'grep -Fq "requires an explicit worktree path" <<<"$out"'
    [ "$fail" = 0 ] && echo "cratis-cleanup-worktrees self-test: $checks checks passed" && exit 0
    echo "$out"; exit 1
fi

# ── Locate the repository ─────────────────────────────────────────────────────
git rev-parse --git-dir >/dev/null 2>&1 || { [ "$QUIET" = 1 ] || echo "cratis-cleanup-worktrees: not in a git repository" >&2; exit "$([ "$HOOK" = 1 ] && echo 0 || echo 2)"; }
here=$(pwd -P)
main_wt=$(git worktree list --porcelain | awk 'NR==1 && $1=="worktree"{sub(/^worktree /,""); print; exit}')
if [ ! -d "$main_wt" ]; then echo "cratis-cleanup-worktrees: cannot find the main worktree" >&2; exit 2; fi
cd "$main_wt" || exit 2

if [ "$FETCH" = 1 ]; then git fetch --prune --quiet origin >/dev/null 2>&1 || LOG_NOFETCH=1; fi
default=$(git symbolic-ref --quiet --short refs/remotes/origin/HEAD 2>/dev/null || true)
[ -n "$default" ] || { for c in origin/main origin/master; do git rev-parse -q --verify "refs/remotes/$c" >/dev/null && default=$c && break; done; }
[ -n "$default" ] || { [ "$QUIET" = 1 ] || echo "cratis-cleanup-worktrees: no origin default branch; nothing to compare against" >&2; exit "$([ "$HOOK" = 1 ] && echo 0 || echo 2)"; }
default_local=${default#origin/}

firstparent=$(mktemp "${TMPDIR:-/tmp}/cleanup-fp.XXXXXX"); trap 'rm -f "$firstparent"' EXIT
git rev-list --first-parent "$default" > "$firstparent" 2>/dev/null

REMOVED=(); KEPT=(); FREED_KB=0
note_removed() { REMOVED+=("$1"); }
note_kept() { KEPT+=("$1"); }

# The cache is available only after a successful, valid batch. Failed attempts also stamp the window.
GH_PULLS=''
valid_pulls() {
    jq -e -s 'length == 1 and (.[0] | type == "array" and all(.[];
        type == "object" and has("merged_at") and (.merged_at == null or (.merged_at | type == "string")) and
        (.head | type == "object" and (.ref | type == "string") and (.sha | type == "string"))))' "$1" >/dev/null 2>&1
}
lock_stat() { stat -c '%i %Y' "$1" 2>/dev/null || stat -f '%i %m' "$1" 2>/dev/null; }
fresh_stamp() {
    local stamp
    stamp=$(cat "$state/stamp" 2>/dev/null || true)
    [[ "$stamp" =~ ^[0-9]+$ ]] && [ "$now" -lt "$((stamp + 900))" ]
}
cached_pulls() {
    fresh_stamp && [ ! -e "$state/failed" ] && valid_pulls "$state/pulls.json" || return 1
    cat "$state/pulls.json"
}
take_lock() {
    mkdir "$state/lock" 2>/dev/null && return 0
    local inode modified retired acquired current
    read -r inode modified < <(lock_stat "$state/lock")
    [[ "$modified" =~ ^[0-9]+$ ]] && [ "$now" -ge "$((modified + 900))" ] || return 1
    # Rename atomically: a killed reclaimer can leave only a retired directory, never a blocking marker.
    retired="$state/lock.stale.$$.$RANDOM"
    mv "$state/lock" "$retired" 2>/dev/null || return 1
    read -r current _ < <(lock_stat "$retired")
    if [ "$current" != "$inode" ]; then
        # Another reclaimer replaced the stale directory. Restore its lock if possible;
        # never acquire or delete a directory that was not the stale one we inspected.
        [ -e "$state/lock" ] || mv "$retired" "$state/lock" 2>/dev/null
        return 1
    fi
    acquired=1
    mkdir "$state/lock" 2>/dev/null && acquired=0
    rm -rf "$retired"
    return "$acquired"
}
hook_pulls() (
    local repo="$1" common state now temporary lock_inode current
    common=$(git rev-parse --git-common-dir) || exit 1
    state="$common/cratis-cleanup-gh/$repo"
    now=$(date +%s)
    mkdir -p "$state" 2>/dev/null || exit 1
    if ! take_lock; then cached_pulls; exit $?; fi
    read -r lock_inode _ < <(lock_stat "$state/lock")
    temporary=''
    # A retired owner must never remove its replacement's lock.
    trap '[[ -z "$temporary" ]] || rm -f "$temporary"; read -r current _ < <(lock_stat "$state/lock"); [ "$current" != "$lock_inode" ] || rmdir "$state/lock" 2>/dev/null' EXIT
    if fresh_stamp; then cached_pulls; exit $?; fi
    temporary=$(mktemp "$state/pulls.XXXXXX") || exit 1
    printf '%s\n' "$now" > "$temporary" && mv "$temporary" "$state/failed" || exit 1
    printf '%s\n' "$now" > "$temporary" && mv "$temporary" "$state/stamp" || exit 1
    read -r current _ < <(lock_stat "$state/lock")
    [ "$current" = "$lock_inode" ] || exit 1
    # Only the latest 100 closed PRs are searched; older merged branches are simply kept.
    if gh api "repos/$repo/pulls?state=closed&per_page=100" > "$temporary" 2>/dev/null && valid_pulls "$temporary"; then
        read -r current _ < <(lock_stat "$state/lock")
        [ "$current" = "$lock_inode" ] || exit 1
        mv "$temporary" "$state/pulls.json" && rm -f "$state/failed" || exit 1
        cat "$state/pulls.json"
    else exit 1; fi
)
GH_POSSIBLE=0; GH_REPO=''
configure_github() {
    [ "${CRATIS_CLEANUP_NO_GH:-}" != 1 ] || return 0
    command -v gh >/dev/null 2>&1 && command -v jq >/dev/null 2>&1 || return 0
    local remote repo
    remote=$(git remote get-url origin 2>/dev/null) || return 0
    case "$remote" in
        https://github.com/*) repo=${remote#https://github.com/} ;;
        git@github.com:*) repo=${remote#git@github.com:} ;;
        ssh://git@github.com/*) repo=${remote#ssh://git@github.com/} ;;
        *) return 0 ;;
    esac
    repo=${repo%.git}
    [[ "$repo" =~ ^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$ ]] || return 0
    GH_REPO=$repo; GH_POSSIBLE=1
}
configure_github
load_pulls() {
    if [ "$HOOK" = 1 ]; then GH_PULLS=$(hook_pulls "$GH_REPO") || return 1
    else GH_PULLS=$(gh api "repos/$GH_REPO/pulls?state=closed&per_page=100" 2>/dev/null) || return 1; fi
    printf '%s' "$GH_PULLS" | valid_pulls /dev/stdin
}

# Ancestry evidence is resolved during planning; GitHub evidence only after the complete plan exists.
ancestry_delivered() {
    local sha="$1" track="$3"
    if git merge-base --is-ancestor "$sha" "$default" 2>/dev/null; then
        if ! grep -qx "$sha" "$firstparent"; then EVIDENCE="merged into $default"; return 0; fi
        if [ "$track" = "[gone]" ]; then EVIDENCE="merged into $default, upstream gone"; return 0; fi
    fi
    return 1
}
github_delivered() {
    printf '%s' "$GH_PULLS" | jq -e --arg br "$1" --arg sha "$2" \
        'any(.[]; .merged_at != null and .head.ref == $br and .head.sha == $sha)' >/dev/null 2>&1
}
PLAN_KIND=(); PLAN_PATH=(); PLAN_SHA=(); PLAN_BRANCH=(); PLAN_EVIDENCE=(); NEEDS_GH=0
plan_item() {
    local kind="$1" path="$2" tip="$3" branch="$4" evidence=''
    if ancestry_delivered "$tip" "$branch" "$([ -n "$branch" ] && track_of "$branch")"; then evidence=$EVIDENCE
    elif [ -n "$branch" ] && [ "$GH_POSSIBLE" = 1 ]; then NEEDS_GH=1; fi
    PLAN_KIND+=("$kind"); PLAN_PATH+=("$path"); PLAN_SHA+=("$tip")
    PLAN_BRANCH+=("$branch"); PLAN_EVIDENCE+=("$evidence")
}
track_of() { git for-each-ref --format='%(upstream:track)' "refs/heads/$1" 2>/dev/null; }

# ── Plan the entire sweep without removing worktrees or branches ──────────────
wt=""; sha=""; br=""; locked=0; first=1
process_worktree() {
    [ -n "$wt" ] || return 0
    if [ "$first" = 1 ]; then first=0; return 0; fi        # the main worktree is never removed
    local name="${br:-detached}"
    if [ "$locked" = 1 ]; then note_kept "worktree $wt ($name): locked"; return 0; fi
    case "$here/" in "$wt"/*) note_kept "worktree $wt ($name): it is the current directory - run the sweep from the main checkout"; return 0 ;; esac
    plan_item worktree "$wt" "$sha" "$br"
}
# $$ remains the script's root PID in Bash subshells, including Bash 3.2 without BASHPID.
# Ancestors are deliberately not excluded: their cwd or open files may hold another worktree.
check_pi_holders() {
    local target="$1" processes="$2" report code own
    report=$(pi-holders --json "$target" 2>/dev/null); code=$?
    if [ "$code" -gt 1 ]; then HOLDER_REASON="live holder or failed holder check (pi-holders)"; return 1; fi
    command -v jq >/dev/null 2>&1 || { HOLDER_REASON="holder check unavailable (pi-holders JSON requires jq)"; return 1; }
    own=$(awk -v root="$$" '
        { parent[$1] = $2 }
        END {
            for (pid in parent) {
                ancestor = pid
                while (ancestor > 0 && ancestor != root) ancestor = parent[ancestor]
                if (ancestor == root) print pid
            }
        }
    ' <<< "$processes") || { HOLDER_REASON="holder check failed (process inspection)"; return 1; }
    if ! jq -s -e --arg target "$target" --arg own "$own" --argjson code "$code" '
        def positive_pid: tostring | test("^[1-9][0-9]*$");
        def holder:
            type == "object" and (.pid | positive_pid) and (.command | type == "string") and
            (.kind == "cwd" or .kind == "open") and
            (.name | type == "string" and (. == $target or startswith($target + "/")));
        ($own | split("\n")) as $excluded |
        length == 1 and (.[0] |
            type == "object" and (.status == "clear" or .status == "held") and
            ($code == (if .status == "held" then 1 else 0 end)) and
            (.paths | type == "array" and length == 1) and
            (.paths[0] |
                .path == $target and .resolved == $target and (.held | type == "boolean") and
                (.holders | type == "array" and all(.[]; holder)) and
                (.held == (.holders | length > 0)) and .ambiguous == 0) and
            (.paths[0].held == (.status == "held")) and
            all(.paths[0].holders[]; (.pid | tostring) as $pid | $excluded | index($pid) != null)
        )
    ' <<< "$report" >/dev/null 2>&1; then
        HOLDER_REASON="live holder or invalid/ambiguous holder check (pi-holders)"; return 1
    fi
    return 0
}
# Parse names, not inode traversal. Incomplete names inside the target (or without any path) are
# unsafe; incomplete records naming other absolute paths cannot hide a target name in that record.
# This avoids unrelated FD-info errors from process churn poisoning every candidate.
lsof_matches() {
    local target="$1" processes="$2" listing="$3"
    awk -v root="$$" -v target="$target" '
        function under_target(path) { return path == target || index(path, target "/") == 1 }
        function unsafe(detail) { if (!bad) first_bad = detail; bad = 1 }
        NR == FNR { parent[$1] = $2; next }
        /^p[1-9][0-9]*$/ {
            pid = substr($0, 2); ignored = 0; ancestor = pid; kind = ""; type = ""; seen_pid = 1
            while (ancestor > 0 && !ignored) {
                if (ancestor == root) ignored = 1
                ancestor = parent[ancestor]
            }
            next
        }
        /^f./ { kind = substr($0, 2); type = ""; next }
        /^t./ { type = substr($0, 2); next }
        /^n/ {
            seen_name = 1; path = substr($0, 2)
            if (ignored || path == "(revoked)") next
            # A diagnostic suffix can follow the target directory itself, not just a child path.
            sub(/ (\(stat:|\(FD info error:|FD info error:).*/, "", path)
            if (under_target(path)) {
                if (!seen_pid || kind == "" || type == "") unsafe("incomplete " kind " " type " " path)
                else print kind "|" path
            } else if (path !~ /^\//) {
                # Kernel/network object names are not filesystem paths. Unix sockets may have paths,
                # so only their non-path addresses are exempt; unknown names may hide a target file.
                if (kind != "" && kind != "cwd" && type ~ /^(rte|ndrv)$/) next
                if (kind != "cwd" && type ~ /^(PIPE|NPOLICY|NEXUS|KQUEUE|IPv4|IPv6|systm|CHAN)$/) next
                if (kind != "cwd" && type == "unix" && path ~ /^(->0x[[:xdigit:]]+|->\(none\)|@.*|[[:alnum:]_.-]*)$/) next
                # Linux anonymous objects have typed pseudo-names, not filesystem paths.
                # Require a descriptor and a known type/name pair; cwd and incomplete records stay unsafe.
                if (kind != "" && kind != "cwd") {
                    if (type == "FIFO" && path ~ /^pipe(:\[[0-9]+\])?$/) next
                    if (type == "sock" && path ~ /^(socket:\[[0-9]+\]|protocol: [[:alnum:]_-]+)$/) next
                    # Anonymous-inode names are kernel objects whatever the reported type (for example a
                    # memory-mapped `anon_inode:[io_uring]` is type REG), so the name alone exempts them.
                    if (path ~ /^(anon_inode:)?\[[[:alnum:]_]+(:[0-9]+(,[0-9]+)*)?\]$/) next
                    # lsof truncates long names, leaving a digit/comma list ending in `...` with no closing bracket.
                    if (path ~ /^(anon_inode:)?\[[[:alnum:]_]+:[0-9,]*\.\.\.$/) next
                    if (type == "a_inode" && path ~ /^(anon_inode:)?[[:alnum:]_]+$/) next
                    if (type == "unix" && path ~ /^type=[[:upper:]]+( \([[:upper:]]+\))?$/) next
                    if (type == "netlink" && path ~ /^[[:upper:]_]+$/) next
                }
                unsafe("unrecognised " kind " " type " " path)
            }
            next
        }
        { if (!ignored) unsafe("unexpected record " $0) }
        END {
            if (!seen_pid || !seen_name) unsafe("empty listing")
            if (bad) { print "!" first_bad; exit 1 }
        }
    ' <(printf '%s\n' "$processes") <(printf '%s\n' "$listing")
}
# One process-wide listing per candidate, retried at most three times before failing closed.
check_lsof_holders() {
    local target="$1" processes="$2" listing matches detail kind path links attempt
    for attempt in 1 2 3; do
        if ! listing=$(lsof -nP -w -F pfnt 2>/dev/null); then
            HOLDER_REASON="holder check failed (lsof)"; continue
        fi
        if matches=$(lsof_matches "$target" "$processes" "$listing"); then HOLDER_REASON=''; break; fi
        # The first unsafe record is the last output line; builtins only (PATH may be minimal).
        detail=${matches##*$'\n'}
        HOLDER_REASON="holder check failed (path inspection: ${detail#!})"
    done
    [ "$attempt" != 3 ] || [ -z "${HOLDER_REASON:-}" ] || return 1
    while IFS='|' read -r kind path; do
        [ -n "$path" ] || continue
        # Shared hard-link names cannot identify which worktree the process actually holds.
        if [ "$kind" != cwd ] && [ -f "$path" ]; then
            links=$(stat -c '%h' "$path" 2>/dev/null || stat -f '%l' "$path" 2>/dev/null) || links=0
            if ! [[ "$links" =~ ^[0-9]+$ ]] || [ "$links" != 1 ]; then
                HOLDER_REASON="ambiguous holder check (shared or unknown inode: $path)"; return 1
            fi
        fi
        HOLDER_REASON="live process holds worktree ($kind: $path)"; return 1
    done <<< "$matches"
    return 0
}
check_holders() {
    local target processes use_pi=0
    HOLDER_REASON=''
    target=$(cd "$1" 2>/dev/null && pwd -P) || { HOLDER_REASON="could not resolve worktree for holder check"; return 1; }
    if command -v pi-holders >/dev/null 2>&1 && command -v jq >/dev/null 2>&1; then use_pi=1
    elif ! command -v lsof >/dev/null 2>&1; then
        HOLDER_REASON="no holder check tool available (pi-holders+jq/lsof)"; return 1
    fi
    processes=$(ps -axo pid=,ppid= 2>/dev/null) || { HOLDER_REASON="holder check failed (ps)"; return 1; }
    if ! awk -v root="$$" '$1 == root { found = 1 } END { exit !found }' <<< "$processes"; then
        HOLDER_REASON="holder check incomplete (ps)"; return 1
    fi
    if [ "$use_pi" = 1 ]; then check_pi_holders "$target" "$processes"
    else check_lsof_holders "$target" "$processes"; fi
}
include_work_records() {
    [ "$HOOK" = 0 ] || return 1
    local path target
    target=$(cd "$1" 2>/dev/null && pwd -P) || return 1
    for path in "${WORK_RECORD_PATHS[@]+"${WORK_RECORD_PATHS[@]}"}"; do
        [ "$path" != "$target" ] || return 0
    done
    return 1
}
worktree_clean() {
    local status
    if ! status=$(git -C "$wt" status --porcelain --ignored 2>/dev/null); then
        note_kept "worktree $wt ($name): could not inspect worktree status"; return 1
    fi
    if [[ "$status" == '?? '* || "$status" == '!! '* || "$status" == *$'\n?? '* || "$status" == *$'\n!! '* ]]; then
        note_kept "worktree $wt ($name): has ignored/untracked files; archive or remove them first"; return 1
    fi
    if [ -n "$status" ]; then note_kept "worktree $wt ($name): delivered but has uncommitted changes"; return 1; fi
    return 0
}
execute_worktree() {
    local name="${br:-detached}" ev="$1" current
    current=$(git -C "$wt" rev-parse HEAD 2>/dev/null) || return 0
    [ "$current" = "$sha" ] || { note_kept "worktree $wt ($name): tip changed since planning"; return 0; }
    if ! include_work_records "$wt" && [ -d "$wt/.ai-work" ]; then
        local records
        if ! records=$(find "$wt/.ai-work/." -mindepth 1 -maxdepth 1 -print -quit); then
            note_kept "worktree $wt ($name): could not inspect work records (.ai-work/)"; return 0
        fi
        if [ -n "$records" ]; then note_kept "worktree $wt ($name): has work records (.ai-work/)"; return 0; fi
    fi
    worktree_clean || return 0
    local kb; kb=$(du -sk "$wt" 2>/dev/null | awk '{print $1}')
    if ! check_holders "$wt"; then note_kept "worktree $wt ($name): $HOLDER_REASON"; return 0; fi
    worktree_clean || return 0
    if [ "$DRY" = 1 ]; then note_removed "worktree $wt ($name): would remove, $ev"; return 0; fi
    if git worktree remove "$wt" >/dev/null 2>&1; then
        FREED_KB=$((FREED_KB + ${kb:-0})); note_removed "worktree $wt ($name): $ev"
    else note_kept "worktree $wt ($name): git refused to remove it"; fi
}
while IFS= read -r line; do
    case "$line" in
        "worktree "*) process_worktree; wt=${line#worktree }; sha=""; br=""; locked=0 ;;
        "HEAD "*) sha=${line#HEAD } ;;
        "branch "*) br=${line#branch refs/heads/} ;;
        "locked"*) locked=1 ;;
    esac
done < <(git worktree list --porcelain; echo)
process_worktree

in_use=$(git worktree list --porcelain | sed -n 's#^branch refs/heads/##p')
planned_branches=$(printf '%s\n' "${PLAN_BRANCH[@]+"${PLAN_BRANCH[@]}"}")
while IFS='|' read -r b bsha; do
    if [ -z "$b" ] || [ "$b" = "$default_local" ]; then continue; fi
    # Drain producers under pipefail: grep -q can turn a match into a SIGPIPE failure.
    if printf '%s\n' "$in_use" | grep -x "$b" >/dev/null; then
        [ "$DRY" = 0 ] || continue
        printf '%s\n' "$planned_branches" | grep -x "$b" >/dev/null || continue
    fi
    plan_item branch "$b" "$bsha" "$b"
done < <(git for-each-ref --format='%(refname:short)|%(objectname)' refs/heads)

abort_sweep() {
    echo "cratis-cleanup-worktrees: GitHub evidence unavailable; 0 removed, keeping the entire sweep" >&2
    exit 0
}
# Resolve every item's evidence before any deletion, including ancestry-proven items.
if [ "$NEEDS_GH" = 1 ] && ! load_pulls; then abort_sweep; fi
for ((i=0; i<${#PLAN_KIND[@]}; i++)); do
    if [ -z "${PLAN_EVIDENCE[i]}" ] && [ -n "${PLAN_BRANCH[i]}" ] && [ -n "$GH_PULLS" ]; then
        if github_delivered "${PLAN_BRANCH[i]}" "${PLAN_SHA[i]}"; then PLAN_EVIDENCE[i]="pull request merged at this commit"
        else result=$?; [ "$result" = 1 ] || abort_sweep; fi
    fi
done

# ── Execute the resolved plan, rechecking tips and worktree safety ─────────────
git worktree prune >/dev/null 2>&1
for ((i=0; i<${#PLAN_KIND[@]}; i++)); do
    kind=${PLAN_KIND[i]}; path=${PLAN_PATH[i]}; sha=${PLAN_SHA[i]}; br=${PLAN_BRANCH[i]}; ev=${PLAN_EVIDENCE[i]}
    if [ "$kind" = branch ]; then
        git worktree list --porcelain | sed -n 's#^branch refs/heads/##p' | grep -x "$br" >/dev/null && continue
    fi
    if [ -z "$ev" ]; then
        if [ "$kind" = worktree ]; then note_kept "worktree $path (${br:-detached}): work not delivered to $default"
        else note_kept "branch $br: work not delivered to $default"; fi
        continue
    fi
    if [ "$kind" = worktree ]; then wt=$path; execute_worktree "$ev"; continue; fi
    [ "$(git rev-parse -q --verify "refs/heads/$br")" = "$sha" ] || { note_kept "branch $br: tip changed since planning"; continue; }
    if [ "$DRY" = 1 ]; then note_removed "branch $br: would delete, $ev"
    elif git branch -D "$br" >/dev/null 2>&1; then note_removed "branch $br: $ev"
    else note_kept "branch $br: git refused to delete it"; fi
done

# ── Report ────────────────────────────────────────────────────────────────────
summary="cratis-cleanup-worktrees: ${#REMOVED[@]} removed, ${#KEPT[@]} kept"
[ "$FREED_KB" -gt 0 ] && summary="$summary, $((FREED_KB / 1024)) MB freed"
[ "${LOG_NOFETCH:-0}" = 1 ] && summary="$summary (fetch failed: compared against the last fetched $default)"
if [ "$HOOK" = 1 ]; then
    [ "${#REMOVED[@]}" -gt 0 ] || { [ "$EVENT" = "SessionStart" ] && [ "${#KEPT[@]}" -gt 0 ]; } || exit 0
    msg="$summary."
    [ "${#KEPT[@]}" -gt 0 ] && msg="$msg Kept (not delivered or not safe to remove): $(printf '%s; ' "${KEPT[@]}")"
    command -v jq >/dev/null 2>&1 && jq -nc --arg m "$msg" --arg e "$EVENT" '{systemMessage:$m,hookSpecificOutput:{hookEventName:$e,additionalContext:$m}}' || printf '%s\n' "$msg" >&2
    exit 0
fi
if [ "$QUIET" = 0 ]; then
    echo "$summary"
    for r in "${REMOVED[@]+"${REMOVED[@]}"}"; do echo "  removed $r"; done
    for k in "${KEPT[@]+"${KEPT[@]}"}"; do echo "  kept    $k"; done
fi
exit 0
