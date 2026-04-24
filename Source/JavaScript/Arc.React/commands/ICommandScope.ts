// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ICommand, CommandResults } from '@cratis/arc/commands';
import { ValidationResult } from '@cratis/arc/validation';
import { IQueryFor } from '@cratis/arc/queries';

/**
 * Defines the system for tracking commands and queries in a scope.
 * This is an abstract class that can be extended to implement custom behavior.
 */
export abstract class ICommandScope {
    /**
     * Gets the parent scope, if any.
     */
    abstract get parent(): ICommandScope | undefined;

    /**
     * Gets whether or not there are any changes in this scope or any child scopes.
     */
    abstract get hasChanges(): boolean;

    /**
     * Gets whether or not any commands or queries are currently being performed in this scope or any child scopes.
     */
    abstract get isPerforming(): boolean;

    /**
     * Gets whether or not any commands in this scope or any child scopes have validation failures from the last execution.
     */
    abstract get hasValidationFailures(): boolean;

    /**
     * Gets whether or not any commands in this scope or any child scopes have exceptions from the last execution.
     */
    abstract get hasExceptions(): boolean;

    /**
     * Gets the validation failures per command for this scope's commands.
     */
    abstract get validationFailures(): ReadonlyMap<ICommand, ValidationResult[]>;

    /**
     * Gets aggregated validation failures for this scope and all child scopes.
     */
    abstract get aggregatedValidationFailures(): ReadonlyArray<ValidationResult>;

    /**
     * Gets the exception messages per command for this scope's commands.
     */
    abstract get exceptions(): ReadonlyMap<ICommand, string[]>;

    /**
     * Gets aggregated exception messages for this scope and all child scopes.
     */
    abstract get aggregatedExceptions(): ReadonlyArray<string>;

    /**
     * Add a command for tracking in the scope.
     * @param {ICommand} command Command to add.
     */
    abstract addCommand(command: ICommand): void;

    /**
     * Add a query for tracking in the scope.
     * @param {IQueryFor<unknown, unknown>} query Query to add.
     */
    abstract addQuery(query: IQueryFor<unknown, unknown>): void;

    /**
     * Add a child scope to this scope for aggregate state tracking.
     * @param {ICommandScope} scope Child scope to add.
     */
    abstract addChildScope(scope: ICommandScope): void;

    /**
     * Execute all commands with changes.
     * @returns {Promise<CommandResults>} Command results per command that was executed.
     */
    abstract execute(): Promise<CommandResults>;

    /**
     * Revert any changes for commands in scope.
     */
    abstract revertChanges(): void;
}
