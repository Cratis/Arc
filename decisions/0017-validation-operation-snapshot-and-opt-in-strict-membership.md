---
id: 0017-validation-operation-snapshot-and-opt-in-strict-membership
title: Give validators a fail-closed operation snapshot and make strict membership opt-in
status: accepted
stage: implemented
decided: 2026-11-02
decider: woksin (via L0)
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Arc.Core/Validation/**
  - Source/DotNET/Arc.Core/Commands/**
  - Source/DotNET/Arc.Core/Queries/**
  - Source/DotNET/Arc.Core/Tenancy/**
  - Documentation/backend/csharp/commands/validation.md
  - Documentation/backend/csharp/tenancy/membership.md
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

Validators had no supported way to read the operation's tenant and caller, so authors injected an accessor and read ambient state that a nested operation or a tenant switch could change. A `TenantMembershipDenied` thrown inside a validator was turned into a `ValidatorFailed` validation error, which a raised allowed severity can drop.

## Decision

Each command or query pipeline begins one operation snapshot, carried to validators in the validation context and read through `GetTenant()`, `GetCausedBy()` and `IsSystem()`. A read outside a pipeline, a denied membership and an in-place change to the caller's claims all reject the operation as unauthorized, and severity settings cannot hide it. `Tenancy:StrictValidatorMembership` makes a membership denial thrown by any validator unauthorized too. It is off by default and stays off until an owner-approved major version.

## Options considered

- **Snapshot plus opt-in strict mode (chosen).** Adds the safe read path and changes no existing behavior.
- **Strict by default.** Closes the gap for everyone but changes behavior for existing validators.
- **Ambient accessor only.** Leaves validators exposed to nested operations and tenant switches.

## Default if unanswered

Validators keep reading ambient state, and a membership denial thrown by a validator can be dropped by a raised allowed severity.

## Timeline and scope

Apply now to Cratis/Arc#3582. No default changes; the option is additive.

## Verification

**Done when:** a validator reads the tenant and caller through the validation context, and a read outside a pipeline or for a denied caller is unauthorized.

**Verify by:** the `for_OperationSnapshot`, `for_ValidatorInvoker` and `for_CommandPipeline` specs.

## Consequences

Applications should enable `Tenancy:StrictValidatorMembership`. The `CausingIdentity` is Arc's view of the caller, not the Chronicle audit identity.
