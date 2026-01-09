// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from '@cratis/fundamentals';
import { useState, useCallback, useContext, useRef, useMemo } from 'react';
import { Command } from '@cratis/arc/commands';
import React from 'react';
import { CommandScopeContext } from './CommandScope';
import { ArcContext } from '../ArcContext';

export type SetCommandValues<TCommandContent> = (command: TCommandContent) => void;
export type ClearCommandValues = () => void;

/**
 * Use a command in a component.
 * @param commandType Type of the command to use.
 * @param initialValues Any initial values to set for the command.
 * @returns Tuple with the command, a {@link SetCommandValues<TCommandContent>} delegate to set values on command and {@link ClearCommandValues} delegate clear values.
 */
export function useCommand<
    TCommand extends Command<TCommandContent, TCommandResponse>,
    TCommandContent = object,
    TCommandResponse = object
>(
    commandType: Constructor<TCommand>,
    initialValues?: TCommandContent
): [TCommand, SetCommandValues<TCommandContent>, ClearCommandValues] {
    const command = useRef<TCommand | null>(null);
    const [hasChanges, setHasChanges] = useState(false);
    const arc = useContext(ArcContext);

    const propertyChangedCallback = useCallback(() => {
        if (command.current?.hasChanges !== hasChanges) {
            setHasChanges(command.current?.hasChanges ?? false);
        }
    }, []);

    command.current = useMemo(() => {
        const instance = new commandType();
        instance.setMicroservice(arc.microservice);
        instance.setApiBasePath(arc.apiBasePath ?? '');
        instance.setOrigin(arc.origin ?? '');
        instance.setHttpHeadersCallback(arc.httpHeadersCallback ?? (() => ({})));
        if (initialValues) {
            instance.setInitialValues(initialValues);
        }
        instance.onPropertyChanged(propertyChangedCallback, instance);
        return instance;
    }, []);

    const context = React.useContext(CommandScopeContext);
    context.addCommand?.(command.current! as Command<object, object>);

    const setCommandValues = (values: TCommandContent) => {
        const valuesRecord = values as Record<string, unknown>;
        command!.current!.propertyDescriptors.forEach((propertyDescriptor) => {
            if (valuesRecord[propertyDescriptor.name] !== undefined && valuesRecord[propertyDescriptor.name] != null) {
                (command.current as Record<string, unknown>)[propertyDescriptor.name] = valuesRecord[propertyDescriptor.name];
            }
        });
    };

    const clearCommandValues = () => {
        command.current!.propertyDescriptors.forEach((propertyDescriptor) => {
            (command.current as Record<string, unknown>)[propertyDescriptor.name] = undefined;
        });
    };

    return [command.current!, setCommandValues, clearCommandValues];
}
