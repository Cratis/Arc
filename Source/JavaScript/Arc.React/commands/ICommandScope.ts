// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ICommand, CommandResults } from '@cratis/arc/commands';
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
     * Add a command for tracking in the scope.
     * @param {ICommand} command Command to add.
     */
    abstract addCommand(command: ICommand): void;

    /**
     * Add a query for tracking in the scope.
     * @param {IQueryFor<any, any>} query Query to add.
     */
    abstract addQuery(query: IQueryFor<any, any>): void;

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
