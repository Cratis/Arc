// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Defines the system for tracking queries and observable queries in a scope.
 * This is an abstract class that can be extended to implement custom behavior.
 */
export abstract class IQueryScope {
    /**
     * Gets the parent scope, if any.
     */
    abstract get parent(): IQueryScope | undefined;

    /**
     * Gets whether or not any queries or observable queries are currently being performed in this scope or any child scopes.
     */
    abstract get isPerforming(): boolean;

    /**
     * Add a child scope to this scope for aggregate state tracking.
     * @param {IQueryScope} scope Child scope to add.
     */
    abstract addChildScope(scope: IQueryScope): void;

    /**
     * Notify the scope that a query or observable query has started performing.
     * Call this when a query begins executing.
     */
    abstract notifyPerformingStarted(): void;

    /**
     * Notify the scope that a query or observable query has completed performing.
     * Call this when a query finishes executing.
     */
    abstract notifyPerformingCompleted(): void;
}
