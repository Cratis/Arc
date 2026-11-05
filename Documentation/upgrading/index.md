---
title: Upgrade Cratis Arc
description: Check whether your Cratis Arc upgrade needs changes to forwarded identity, discovery access, generated identifiers or command scenarios.
---

When upgrading Cratis Arc, check the guides below for behavior your application relies on. They explain
which code or deployment settings need attention and how to check the result before deploying the upgrade.

| If your application relies on… | Follow this guide |
| --- | --- |
| Forwarded identity headers or anonymous access to discovery endpoints outside Development | [Secure defaults](secure-defaults.mdx): review the ingress trust boundary and choose authenticated or disabled discovery before considering anonymous access. |
| Identifiers created inside `Handle()`, `IIdentitySource` or queued identities in command scenarios | [Generated identifiers](generated-values.mdx): move generation to server-owned parameters, replace queues with named pins and check concept rules. |

Start with the behavior you use; you do not need to change an unrelated part of your application. For a
deployed host, check authentication and discovery using its actual environment and ingress. For generated
values, check both the handler and the scenarios that assert its result.
