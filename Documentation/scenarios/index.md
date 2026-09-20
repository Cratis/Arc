---
title: Scenarios
description: Short, task-oriented recipes for the things you'll actually do with Arc — each assumes you know the basics and gets straight to the goal.
---

These are recipes: "I need to do X — how?" Each one is short and assumes you've built a slice or two (the [tutorial](/arc/tutorial/) is the long way round). For the _why_ and the full set of options, follow the links into the reference. The default recipes use standalone Arc over configured providers or application services. Start with [standalone setup](/arc/backend/csharp/getting-started/); each recipe labels additional prerequisites and illustrative fragments. Chronicle-only behavior is explicitly separated.

| Recipe | When you reach for it |
| --- | --- |
| [Validate a command](./validate-a-command.mdx) | Reject malformed or duplicate input before it writes state |
| [Return a result or an error](./return-a-result-or-error.mdx) | A command needs to hand back more than "it worked" — a value, or a typed failure |
| [Provide data to a command handler](./provide-data-to-a-command.mdx) | A decision needs data you must fetch — keep the fetch out of `Handle` so it stays testable |
| [Use current state in a command](./use-current-state-in-a-command.md) | A decision depends on provider-owned state — resolve it by an explicit command key |
| [Query data across slices](./query-related-data.mdx) | A screen needs data that spans more than one feature |
| [Execute a command from React](./run-a-command-from-react.md) | Wire a form or button to a command through the generated proxy |
| [Test a command](./test-a-command.mdx) | Prove a slice works through the real pipeline — no HTTP, no database |
| [Authorize a command or query](/arc/backend/csharp/authorizing-commands-and-queries/) | Restrict who may run a command or read a query |

Event-sourced Arc slices have their own Chronicle-specific recipes, starting with [React to an event](/arc/backend/csharp/chronicle/react-to-an-event/) and [Add event sourcing to an Arc slice](/arc/backend/csharp/chronicle/add-event-sourcing/).

Missing a recipe you expected? The [Backend](/arc/backend/csharp/) and [Frontend](/arc/frontend/) guides cover the long tail, and [Troubleshooting](/arc/troubleshooting/) catches the common snags.
