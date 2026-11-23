---
name: cratis-engineering-issue-hygiene
description: Use when claiming an issue, posting a meaningful issue event or deciding an authorized closure or necessary follow-up. Not for delivery grouping, review rounds or CI operations.
license: LICENSE
compatibility: Requires git and the GitHub CLI (gh).
---
<!-- cratis-ai-managed: skills/cratis-engineering-issue-hygiene/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Leave fewer issues, with a truthful timeline

An issue must make sense to a reader outside the session. Issues are append-only:
never edit an issue's title, description or any existing comment, including the
agent's own comments. Leave legacy status blocks in issue bodies untouched.
Labels, assignees, milestones and opening or closing issues remain allowed when
authorized. PR descriptions stay editable: they are the release notes, governed
by [pull-requests.md](../../rules/pull-requests.md).

Before any mutation, read
[capability-is-not-authority.md](../../rules/capability-is-not-authority.md).
The request authorizes its named effects, not arbitrary filing or closing.

## One writer and a clear claim

One person or session owns public comments and disposition for a named issue.
Delegated workers report findings to that owner. An explicit handoff can transfer
the named issue, but the previous writer must stop publishing to it. Append-only
comments preserve traceability and avoid concurrent overwrites of issue text.

Before starting, read the issue, comments, assignees and any status block. Follow
[issue-assignment.md](../../rules/issue-assignment.md) and this claiming procedure:

- Applies when the user, branch, task or handover names an issue number in a
  known repository. No known repository or issue means no guessed assignment.
- Read assignees and state in that issue's repository:
  `gh issue view <n> --repo <owner>/<repo> --json assignees,state`.
- If open and unassigned, run
  `gh issue edit <n> --repo <owner>/<repo> --add-assignee @me`.
  `@me` means the authenticated user, not an invented identity.
- Never replace or add to an existing assignment. Skip a closed issue; say so
  without reopening or assigning it.
- Do this once, at the start before the first edit, not on every turn. If it
  fails (permissions or authentication), report it and continue with the work.

Assign each PR you open the same way: `gh pr create --assignee @me`, or
`gh pr edit <n> --add-assignee @me` afterwards.

The request to work on the issue authorizes this assignment only, not comments,
labels or closure. If another writer is active, coordinate rather than starting
parallel publication. Delegated workers do not claim or mutate the issue.

## Append meaningful events, not progress chatter

When comments are authorized, post a NEW self-contained comment only at an event
a reader needs:

- **Started or claimed:** optional and minimal; prefer assignment instead.
- **Pull request opened:** link the public PR and state what it delivers.
- **Blocked or paused:** explain the reason, what remains and what must happen
  before work can resume.
- **Done or closing:** state what shipped, link the delivery and name any
  necessary follow-ups. Pending release is not Done.

Each comment stands alone: what happened, why it matters, what comes next.
Correct outdated information with a new comment linking the earlier one, never
by editing it. A design decision is a new comment; a changed decision is a new
comment superseding the earlier one by link. There is no design-note exception;
read **cratis-engineering-issue-design-note** for that format and trust checks.
Do not post routine progress, repeated status, tool output, test counts, receipts
or model/session chatter. Do not create, refresh or remove a status block.

Use absolute public URLs. Summarize a local plan; never link its file path.
Do not put local paths, harness commands, credentials or private context in
public issue or PR text. Never name a customer or a customer's customer in an
issue, comment or PR; describe the situation generically ("a large enterprise
tenant") and strip identifying products, systems and people. Read
[secrets.md](../../rules/secrets.md) before handling output, and
[local-work-artifacts.md](../../rules/local-work-artifacts.md) before creating a
private comment draft. For an authorized PR-opened event, for example:

```bash
gh issue comment <n> --repo OWNER/REPO --body-file .ai-work/issue-comment.md
gh issue view <n> --repo OWNER/REPO --comments
```

Verify the new comment says what was intended; existing issue text stays intact.

Re-read the issue and PR before every key step: starting, changing scope,
updating public state, filing follow-ups, merging or disposing of the issue.
For example, use `gh issue view <n> --repo OWNER/REPO --comments` and
`gh pr view <pr> --repo OWNER/REPO`. Do not act on an old summary after drift.

## Make the open-issue count go down

1. **Fix before filing.** Fix a small, in-scope finding in the same files and
   risk area as the active change. Do not widen scope to improve the count.
   Review advisories are fixed cheaply inline or dropped, not filed as issues.
2. **File only when needed and authorized.** A real defect that cannot be fixed
   now, or a follow-up delivery truly requires, may need an issue. Search open
   and closed issues first, for example
   `gh issue list --repo OWNER/REPO --state all --search "<area> in:title,body"`.
   Reuse an existing issue instead of duplicating it; comment only when that
   effect is authorized and adds necessary context.
3. Keep every issue generic: no customer or customer-of-customer names, ever.
   Group related findings into one checklist issue per area. Include concrete,
   testable `## Acceptance criteria`. No speculative hardening, ideas or
   "investigate later" issues. If filing is outside the request, list necessary
   follow-ups in the report instead of creating them.
4. **Close what is done**, through the correct delivery mechanism below. Partial
   delivery stays open with exactly what remains. Do not manufacture completion
   or create extra issues merely to move unfinished scope elsewhere.
5. Prefer bounded issues that can be finished completely over starting large
   epics. Report completed dispositions, not how many new issues were produced.

## Close with a reason, through the owning mechanism

Read [pull-requests.md](../../rules/pull-requests.md#issue-references) before
changing issue references or disposition. Confirm the actual PR state, release
intent and repository workflow; a merged PR is not proof of a published release.

- **Release-action delivery:** for a fully delivered issue, the delivering
  release-note bullet ends with `(#n)`. Release-action closes it at release;
  do not close it by hand. If release is pending, keep that dependency visible.
- **Manual completion:** only for a `no-release` PR or a repository without
  release-action, and only when the request includes closing. Confirm all scope
  is delivered and no required PR is open, unmerged, reverted or failing. Set
  Done in a new closing comment and close with reason `completed` and a public
  delivery link.
- **Duplicate or obsolete:** only with clear evidence and authority. Use reason
  `not planned`, explain why, and link the canonical issue or superseding work.
  If uncertain, leave it open and report the question.
- **Partial or unauthorized disposition:** leave it open with truthful remaining
  scope or report the proposed disposition. Do not invent follow-up authority.

For an authorized manual completion, for example:

```bash
gh issue close <n> --repo OWNER/REPO --reason completed \
  --comment "Delivered in <public PR URL>. The agreed behavior is available."
```

A duplicate uses `--reason "not planned"` and a specific canonical public URL.
No closing keywords are needed in the PR description; follow the owning rule.

## End-of-work audit

List issues touched by this work and reconcile each against its PR and release.
For example, `gh issue list --repo OWNER/REPO --assignee @me --state open` helps
find assigned work, but does not replace the list of issues actually touched.
Check merged PRs for completed work still open; distinguish pending release-action
closure from an authorized manual disposition. Verify closure through the issue's
actual state, not an assumption about merging.

Leave no issue appearing actively worked without an active writer. For unfinished
work, post an authorized new Paused or Blocked comment with what remains and what
it waits for; leave any legacy body status untouched. Reconcile follow-ups
with existing issues. Perform only authorized mutations; otherwise report the
proposed changes and the blocker. Summarize completed issues, pending release
closures and unfinished scope without publishing local paths or harness details.

## How it is proven

Read the final title, body, comments and state, for example with
`gh issue view <n> --repo OWNER/REPO --json title,body,comments,state`. The title,
description and pre-existing comments are unchanged; new comments match the
actual PR and release, and any authorized closure is observable. Until
release-action runs, pending closure is not Done.

## What breaks

- Editing existing issue text loses traceability and risks concurrent overwrites.
- Routine progress comments needlessly notify subscribers; meaningful events do not.
- Manual closure before release bypasses release-action's delivery contract.
- Filing every advisory grows the backlog without a necessary delivery.
- Stopping without a paused or blocked event leaves readers expecting active work.
