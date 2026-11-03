// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from '@cratis/fundamentals';
import { useState, useCallback, useContext, useRef, useMemo, useEffect } from 'react';
import { Command } from '@cratis/arc/commands';
import { CommandScopeContext } from './CommandScope.js';
import { ArcContext } from '../ArcContext.js';
import { SliceLocation } from '@cratis/arc/reflection';
import { useRegisterSliceLocation } from '../slices/useRegisterSliceLocation.js';

export type SetCommandValues<TCommandContent> = (command: TCommandContent) => void;
export type ClearCommandValues = () => void;

/**
 * Use a command in a component.
 * Provider configuration updates apply after commit through an effect, without replacing the command.
 * @param commandType Type of the command to use.
 * @param initialValues Any initial values to set for the command.
 * @returns Tuple with the command, a {@link SetCommandValues<TCommandContent>} delegate to set values on command, and a {@link ClearCommandValues} delegate to clear values.
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
    const [, setVersion] = useState(0);
    const arc = useContext(ArcContext);
    useRegisterSliceLocation(SliceLocation.ofType(commandType));

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

    // No layout work is needed: synchronize after commit, never during a render that may be abandoned.
    useEffect(() => {
        command.current!.setMicroservice(arc.microservice);
        command.current!.setApiBasePath(arc.apiBasePath ?? '');
        command.current!.setOrigin(arc.origin ?? '');
        command.current!.setHttpHeadersCallback(arc.httpHeadersCallback ?? (() => ({})));
    }, [arc.microservice, arc.apiBasePath, arc.origin, arc.httpHeadersCallback]);

    const context = useContext(CommandScopeContext);

    useEffect(() => {
        context.addCommand?.(command.current! as Command<object, object>);
    }, [context]);

    const setCommandValues = useCallback((values: TCommandContent) => {
        const valuesRecord = values as Record<string, unknown>;
        command!.current!.propertyDescriptors.forEach((propertyDescriptor) => {
            if (Object.prototype.hasOwnProperty.call(valuesRecord, propertyDescriptor.name)) {
                (command.current as Record<string, unknown>)[propertyDescriptor.name] = valuesRecord[propertyDescriptor.name];
            }
        });
        setVersion(v => v + 1);
    }, []);

    const clearCommandValues = useCallback(() => {
        command.current!.propertyDescriptors.forEach((propertyDescriptor) => {
            (command.current as Record<string, unknown>)[propertyDescriptor.name] = undefined;
        });
        // Increment version to trigger re-renders
        setVersion(v => v + 1);
    }, []);

    return [command.current!, setCommandValues, clearCommandValues];
}
