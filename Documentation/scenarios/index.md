---
title: Scenarios
description: Short, task-oriented recipes for the things you'll actually do with Arc — each assumes you know the basics and gets straight to the goal.
---

These are recipes: "I need to do X — how?" Each one is short and assumes you've built a slice or two (the [tutorial](/arc/tutorial/) is the long way round). For the *why* and the full set of options, follow the links into the reference.

| Recipe | When you reach for it |
|---|---|
| [Validate a command](./validate-a-command.md) | Reject malformed or duplicate input before it writes state |
| [Return a result or an error](./return-a-result-or-error.md) | A command needs to hand back more than "it worked" — a value, or a typed failure |
| [Query data across slices](./query-related-data.md) | A screen needs data that spans more than one feature |
| [Execute a command from React](./run-a-command-from-react.md) | Wire a form or button to a command through the generated proxy |
| [Test a command](./test-a-command.md) | Prove a slice works through the real pipeline — no HTTP, no database |
| [Authorize a command or query](/arc/backend/authorizing-commands-and-queries/) | Restrict who may run a command or read a query |

Event-sourced Arc slices have their own Chronicle-specific recipes, starting with [React to an event](/arc/backend/chronicle/react-to-an-event/).

Missing a recipe you expected? The [Backend](/arc/backend/) and [Frontend](/arc/frontend/) guides cover the long tail, and [Troubleshooting](/arc/troubleshooting/) catches the common snags.
