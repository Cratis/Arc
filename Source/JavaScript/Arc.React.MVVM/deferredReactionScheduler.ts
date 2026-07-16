// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

let pendingReactions = false;

/**
 * A MobX reaction scheduler that defers pending reactions to a microtask so they never run synchronously
 * inside React's render phase.
 *
 * MobX's default scheduler runs reactions synchronously the moment an observable changes. When an observable
 * that a mounted `<Observer>` depends on mutates while React is rendering a *different* component — common
 * when a page mounts many observable queries and one resolves mid-render — the reaction drives `forceUpdate`
 * on that component during render, which React reports as "Cannot update a component while rendering a
 * different component" and can escalate to an error boundary under live streaming timing.
 *
 * Deferring the reaction run to a microtask lets the in-progress render finish first; React then re-renders
 * on the next tick. A single microtask is scheduled per batch — MobX's flush drains every pending reaction,
 * so redundant scheduling is avoided while cascading reactions are still handled.
 * @param runReactions The function provided by MobX that flushes all pending reactions.
 */
export function deferredReactionScheduler(runReactions: () => void): void {
    if (pendingReactions) {
        return;
    }

    pendingReactions = true;
    queueMicrotask(() => {
        pendingReactions = false;
        runReactions();
    });
}
