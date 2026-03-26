// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useEffect, useState, useRef } from 'react';
import { ICommand, CommandResult, CommandResults } from '@cratis/arc/commands';
import { CommandScopeImplementation } from './CommandScopeImplementation';
import { ICommandScope } from './ICommandScope';
import { useCommandScope } from './useCommandScope';

/* eslint-disable @typescript-eslint/no-empty-function */
const defaultCommandScopeContext: ICommandScope = new class extends ICommandScope {
    get parent() { return undefined; }
    get hasChanges() { return false; }
    get isPerforming() { return false; }
    addCommand() { }
    addQuery() { }
    async execute() { return new CommandResults(new Map()); }
    revertChanges() { }
}();
/* eslint-enable @typescript-eslint/no-empty-function */

export const CommandScopeContext = React.createContext<ICommandScope>(defaultCommandScopeContext);

export type CommandScopeChanged = (hasChanges: boolean) => void;
export type CommandScopeExecute = () => Promise<Map<ICommand, CommandResult>>;

export type AddCommand = (command: ICommand) => void;

export interface ICommandScopeProps {
    children?: JSX.Element | JSX.Element[];
    setHasChanges?: CommandScopeChanged;
    setIsPerforming?: (isPerforming: boolean) => void;

    /**
     * Called before each command in the scope is executed.
     * @param {ICommand} command The command that is about to be executed.
     */
    onBeforeExecute?: (command: ICommand) => void;

    /**
     * Called when a command in the scope executes successfully.
     * @param {ICommand} command The command that was executed.
     * @param {CommandResult} result The result of the execution.
     */
    onSuccess?: (command: ICommand, result: CommandResult) => void;

    /**
     * Called when a command in the scope fails for any reason.
     * @param {ICommand} command The command that was executed.
     * @param {CommandResult} result The result of the execution.
     */
    onFailed?: (command: ICommand, result: CommandResult) => void;

    /**
     * Called when a command in the scope fails due to an exception.
     * @param {ICommand} command The command that was executed.
     * @param {CommandResult} result The result of the execution.
     */
    onException?: (command: ICommand, result: CommandResult) => void;

    /**
     * Called when a command in the scope fails due to an authorization failure.
     * @param {ICommand} command The command that was executed.
     * @param {CommandResult} result The result of the execution.
     */
    onUnauthorized?: (command: ICommand, result: CommandResult) => void;

    /**
     * Called when a command in the scope fails due to validation errors.
     * @param {ICommand} command The command that was executed.
     * @param {CommandResult} result The result of the execution.
     */
    onValidationFailure?: (command: ICommand, result: CommandResult) => void;
}

export const CommandScope = (props: ICommandScopeProps) => {
    const parentScope = useCommandScope();
    const [, setHasChanges] = useState(false);
    const [, setIsPerforming] = useState(false);
    const [commandScope, setCommandScope] = useState<ICommandScope>(defaultCommandScopeContext);
    
    // Use refs to capture latest prop values without triggering re-creation
    const setHasChangesRef = useRef(props.setHasChanges);
    const setIsPerformingRef = useRef(props.setIsPerforming);
    const callbacksRef = useRef({
        onBeforeExecute: props.onBeforeExecute,
        onSuccess: props.onSuccess,
        onFailed: props.onFailed,
        onException: props.onException,
        onUnauthorized: props.onUnauthorized,
        onValidationFailure: props.onValidationFailure,
    });
    
    useEffect(() => {
        setHasChangesRef.current = props.setHasChanges;
        setIsPerformingRef.current = props.setIsPerforming;
    }, [props.setHasChanges, props.setIsPerforming]);

    useEffect(() => {
        callbacksRef.current = {
            onBeforeExecute: props.onBeforeExecute,
            onSuccess: props.onSuccess,
            onFailed: props.onFailed,
            onException: props.onException,
            onUnauthorized: props.onUnauthorized,
            onValidationFailure: props.onValidationFailure,
        };
    }, [props.onBeforeExecute, props.onSuccess, props.onFailed, props.onException, props.onUnauthorized, props.onValidationFailure]);

    useEffect(() => {
        const commandScopeImplementation = new CommandScopeImplementation(
            (value) => {
                setHasChanges(value);
                setHasChangesRef.current?.(value);
            },
            (value) => {
                setIsPerforming(value);
                setIsPerformingRef.current?.(value);
            },
            parentScope !== defaultCommandScopeContext ? parentScope : undefined,
            () => callbacksRef.current
        );
        setCommandScope(commandScopeImplementation);
    }, [parentScope]);

    return (
        <CommandScopeContext.Provider value={commandScope}>
            {props.children}
        </CommandScopeContext.Provider>
    );
};
