// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { ICommand, CommandResults } from '@cratis/arc/commands';
import { IQueryFor } from '@cratis/arc/queries';
import { ICommandScope } from './ICommandScope';


/**
 * Represents an implementation of {@link ICommandScope}.
 */
export class CommandScopeImplementation extends ICommandScope {
    private _commands: ICommand[] = [];
    private _queries: IQueryFor<any, any>[] = [];
    private _hasChanges = false;
    private _isPerforming = false;
    private _parent?: ICommandScope;

    constructor(
        private readonly _setHasChanges: (value: boolean) => void,
        private readonly _setIsPerforming?: (value: boolean) => void,
        parent?: ICommandScope
    ) {
        super();
        this._parent = parent;
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
    addCommand(command: ICommand): void {
        if (this._commands.some(_ => _ == command)) {
            return;
        }

        this._commands.push(command);
        this.evaluateState();
        command.onPropertyChanged(this.evaluateState, this);
    }

    /** @inheritdoc */
    addQuery(query: IQueryFor<any, any>): void {
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
                const commandResult = await command.execute();
                commandsToCommandResult.set(command, commandResult);
            }
        } finally {
            this.setIsPerforming(false);
            this.evaluateState();
        }
        
        return new CommandResults(commandsToCommandResult);
    }

    /** @inheritdoc */
    revertChanges() {
        this._commands.forEach(command => command.revertChanges());
        this.evaluateState();
    }

    private setIsPerforming(value: boolean) {
        this._isPerforming = value;
        this._setIsPerforming?.(value);
    }

    private evaluateState() {
        this.evaluateHasChanges();
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
