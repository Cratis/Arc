---
title: Cratis Arc task recipes
description: Build and check a Cratis Arc feature with recipes for React commands, validation, current-state decisions, related-data queries, and tests.
---

Cratis Arc task recipes help you add or check one part of an application feature: submit a command, reject invalid input, or read the data a screen needs. Each recipe explains the setup it assumes, shows a minimal implementation, and gives you an outcome to check.

Start with an Arc application configured for [C#](/arc/backend/csharp/getting-started/), [Kotlin or Java](/arc/backend/kotlin/get-started/), or the [TypeScript backend](/arc/backend/typescript/getting-started/) (available from source; server packages are not published to npm). If you have not built a feature yet, the [tutorial](/arc/tutorial/) walks through the setup and implementation together. The TypeScript [capability reference](/arc/backend/typescript/reference/capabilities/) describes that backend's supported behavior.

For [Arc for Go](/arc/go/), start with its [running application guide](/arc/go/backend/go/core/getting-started/) and [parity reference](/arc/go/parity/). The recipes' backend tabs do not yet include Go examples; use the Go guides for registration, handlers, queries, and generated clients rather than translating framework-specific APIs.

The default recipes use standalone Arc with configured storage providers or application services. They do not require Cratis Chronicle; sections that use its event store identify the additional integration.

| Recipe | When you reach for it |
| --- | --- |
| [Validate a command](./validate-a-command.mdx) | Reject malformed or duplicate input before it writes state |
| [Return a result or an error](./return-a-result-or-error.mdx) | A command needs to hand back more than "it worked" — a value, or a typed failure |
| [Provide data to a command handler](./provide-data-to-a-command.mdx) | A decision needs data you must fetch — keep the fetch out of `Handle` so it stays testable |
| [Use current state in a command](./use-current-state-in-a-command.mdx) | A decision depends on provider-owned state — resolve it by an explicit command key |
| [Query data across slices](./query-related-data.mdx) | A screen needs data that spans more than one feature |
| [Execute a command from React](./run-a-command-from-react.md) | Wire a form or button to a command through the generated proxy |
| [Test a command](./test-a-command.mdx) | Check the pipeline result and handler work in-process, with application services substituted |
| Authorize a command or query: [C#](/arc/backend/csharp/authorizing-commands-and-queries/), [Kotlin or Java](/arc/backend/kotlin/guides/security/), [TypeScript](/arc/backend/typescript/core/authorization/), [Go](/arc/go/backend/go/authorization/) | Restrict who may run a command or read a query; check your backend's authorization model |

If your C# feature records events in Chronicle, continue with its integration recipes: [React to an event](/arc/backend/csharp/chronicle/react-to-an-event/) and [Add event sourcing to an Arc slice](/arc/backend/csharp/chronicle/add-event-sourcing/).

For a task not listed here, use the [backend options](/arc/backend/) or [Frontend](/arc/frontend/) guides. If an implementation does not behave as expected, start with [Troubleshooting](/arc/troubleshooting/).
