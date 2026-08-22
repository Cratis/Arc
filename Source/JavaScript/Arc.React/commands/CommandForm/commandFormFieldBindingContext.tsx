// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';

const CommandFormFieldBindingContext = React.createContext(false);

/**
 * Marks the exact field clone produced by CommandFormFieldWrapper as framework-bound.
 * @param children The cloned field implementation.
 * @returns The binding marker provider around the field.
 */
export const CommandFormFieldBinding = ({ children }: { children: React.ReactNode }) => (
    <CommandFormFieldBindingContext.Provider value>
        {children}
    </CommandFormFieldBindingContext.Provider>
);

/**
 * Indicates whether the current runtime-binding field is the clone owned by CommandFormFieldWrapper.
 * @returns True only inside the framework binding marker.
 */
export const useIsCommandFormFieldBound = (): boolean =>
    React.useContext(CommandFormFieldBindingContext);
