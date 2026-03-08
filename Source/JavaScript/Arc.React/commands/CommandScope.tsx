// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useEffect, useState, useRef } from 'react';
import { Command, CommandResult, CommandResults } from '@cratis/arc/commands';
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
export type CommandScopeExecute = () => Promise<Map<Command, CommandResult>>;

export type AddCommand = (command: Command) => void;

export interface ICommandScopeProps {
    children?: JSX.Element | JSX.Element[];
    setHasChanges?: CommandScopeChanged;
    setIsPerforming?: (isPerforming: boolean) => void;
}

export const CommandScope = (props: ICommandScopeProps) => {
    const parentScope = useCommandScope();
    const [hasChanges, setHasChanges] = useState(false);
    const [isPerforming, setIsPerforming] = useState(false);
    const [commandScope, setCommandScope] = useState<ICommandScope>(defaultCommandScopeContext);
    
    // Use refs to capture latest prop values without triggering re-creation
    const setHasChangesRef = useRef(props.setHasChanges);
    const setIsPerformingRef = useRef(props.setIsPerforming);
    
    useEffect(() => {
        setHasChangesRef.current = props.setHasChanges;
        setIsPerformingRef.current = props.setIsPerforming;
    }, [props.setHasChanges, props.setIsPerforming]);

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
            parentScope !== defaultCommandScopeContext ? parentScope : undefined
        );
        setCommandScope(commandScopeImplementation);
    }, [parentScope]);

    return (
        <CommandScopeContext.Provider value={commandScope}>
            {props.children}
        </CommandScopeContext.Provider>
    );
};
