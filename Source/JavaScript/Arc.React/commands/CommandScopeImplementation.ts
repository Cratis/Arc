// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ICommand, CommandResult, CommandResults } from '@cratis/arc/commands';
import { ValidationResult } from '@cratis/arc/validation';
import { IQueryFor } from '@cratis/arc/queries';
import { ICommandScope } from './ICommandScope';

/**
 * Defines the callbacks that can be provided to a {@link CommandScopeImplementation}.
 */
export interface CommandScopeCallbacks {
    /**
     * Called before each command is executed.
     * @param {ICommand} command The command that is about to be executed.
     */
    onBeforeExecute?: (command: ICommand) => void;

    /**
     * Called when a command executes successfully.
     * @param {ICommand} command The command that was executed.
     * @param {CommandResult} result The result of the execution.
     */
    onSuccess?: (command: ICommand, result: CommandResult) => void;

    /**
     * Called when a command fails for any reason.
     * @param {ICommand} command The command that was executed.
     * @param {CommandResult} result The result of the execution.
     */
    onFailed?: (command: ICommand, result: CommandResult) => void;

    /**
     * Called when a command fails due to an exception.
     * @param {ICommand} command The command that was executed.
     * @param {CommandResult} result The result of the execution.
     */
    onException?: (command: ICommand, result: CommandResult) => void;

    /**
     * Called when a command fails due to an authorization failure.
     * @param {ICommand} command The command that was executed.
     * @param {CommandResult} result The result of the execution.
     */
    onUnauthorized?: (command: ICommand, result: CommandResult) => void;

    /**
     * Called when a command fails due to validation errors.
     * @param {ICommand} command The command that was executed.
     * @param {CommandResult} result The result of the execution.
     */
    onValidationFailure?: (command: ICommand, result: CommandResult) => void;
}

/**
 * Represents an implementation of {@link ICommandScope}.
 */
export class CommandScopeImplementation extends ICommandScope {
    private _commands: ICommand[] = [];
    private _queries: IQueryFor<unknown, unknown>[] = [];
    private _childScopes: ICommandScope[] = [];
    private _hasChanges = false;
    private _isPerforming = false;
    private _parent?: ICommandScope;
    private _validationFailures: Map<ICommand, ValidationResult[]> = new Map();
    private _exceptions: Map<ICommand, string[]> = new Map();

    constructor(
        private readonly _setHasChanges: (value: boolean) => void,
        private readonly _setIsPerforming?: (value: boolean) => void,
        parent?: ICommandScope,
        private readonly _getCallbacks?: () => CommandScopeCallbacks
    ) {
        super();
        this._parent = parent;
        parent?.addChildScope(this);
    }

    /** @inheritdoc */
    get parent(): ICommandScope | undefined {
        return this._parent;
    }

    /** @inheritdoc */
    get hasChanges(): boolean {
        return this._hasChanges;
    }

    /** @inheritdoc */
    get isPerforming(): boolean {
        return this._isPerforming;
    }

    /** @inheritdoc */
    get hasValidationFailures(): boolean {
        return this._validationFailures.size > 0 || this._childScopes.some(_ => _.hasValidationFailures);
    }

    /** @inheritdoc */
    get hasExceptions(): boolean {
        return this._exceptions.size > 0 || this._childScopes.some(_ => _.hasExceptions);
    }

    /** @inheritdoc */
    get validationFailures(): ReadonlyMap<ICommand, ValidationResult[]> {
        return this._validationFailures;
    }

    /** @inheritdoc */
    get exceptions(): ReadonlyMap<ICommand, string[]> {
        return this._exceptions;
    }

    /** @inheritdoc */
    addChildScope(scope: ICommandScope): void {
        if (this._childScopes.some(_ => _ === scope)) {
            return;
        }
        this._childScopes.push(scope);
    }

    /** @inheritdoc */
    addCommand(command: ICommand): void {
        if (this._commands.some(_ => _ == command)) {
            return;
        }

        this._commands.push(command);
        this.evaluateHasChanges();
        command.onPropertyChanged(this.evaluateHasChanges, this);
    }

    /** @inheritdoc */
    addQuery(query: IQueryFor<unknown, unknown>): void {
        if (this._queries.some(_ => _ == query)) {
            return;
        }

        this._queries.push(query);
    }

    /** @inheritdoc */
    async execute(): Promise<CommandResults> {
        this.setIsPerforming(true);
        const commandsToCommandResult = new Map();

        try {
            for (const command of this._commands.filter(_ => _.hasChanges === true)) {
                this._validationFailures.delete(command);
                this._exceptions.delete(command);

                const callbacks = this._getCallbacks?.();
                callbacks?.onBeforeExecute?.(command);
                const commandResult = await command.execute();
                commandsToCommandResult.set(command, commandResult);

                if (commandResult.isSuccess) {
                    callbacks?.onSuccess?.(command, commandResult);
                } else {
                    callbacks?.onFailed?.(command, commandResult);
                    if (!commandResult.isAuthorized) {
                        callbacks?.onUnauthorized?.(command, commandResult);
                    }
                    if (!commandResult.isValid) {
                        this._validationFailures.set(command, commandResult.validationResults);
                        callbacks?.onValidationFailure?.(command, commandResult);
                    }
                    if (commandResult.hasExceptions) {
                        this._exceptions.set(command, commandResult.exceptionMessages);
                        callbacks?.onException?.(command, commandResult);
                    }
                }
            }
        } finally {
            this.setIsPerforming(false);
            this.evaluateHasChanges();
        }
        
        return new CommandResults(commandsToCommandResult);
    }

    /** @inheritdoc */
    revertChanges() {
        this._commands.forEach(command => command.revertChanges());
        this.evaluateHasChanges();
    }

    private setIsPerforming(value: boolean) {
        this._isPerforming = value;
        this._setIsPerforming?.(value);
    }

    private evaluateHasChanges() {
        let hasCommandChanges = false;
        this._commands.forEach(command => {
            if (command.hasChanges) {
                hasCommandChanges = true;
            }
        });

        this._hasChanges = hasCommandChanges;
        this._setHasChanges(hasCommandChanges);
    }
}
