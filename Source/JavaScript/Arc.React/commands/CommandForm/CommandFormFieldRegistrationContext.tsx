// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import type { CommandFormFieldProps } from './CommandFormField';

export const CommandFormFieldRegistrationContext = React.createContext<
    | {
          register: (
              id: symbol,
              field: React.ReactElement<CommandFormFieldProps>,
          ) => void;
          unregister: (id: symbol) => void;
      }
    | undefined
>(undefined);

/**
 * Registers the field element a CommandFormFieldWrapper actually binds, making rendered field metadata
 * the authoritative source for initial and populated values.
 * @param field The field element being bound.
 */
export function useCommandFormFieldRegistration(
    field: React.ReactElement<CommandFormFieldProps>,
): void {
    const registration = React.useContext(CommandFormFieldRegistrationContext);
    const id = React.useRef(Symbol('CommandFormField'));

    React.useLayoutEffect(() => {
        registration?.register(id.current, field);
    }, [registration, field]);

    React.useLayoutEffect(
        () => () => registration?.unregister(id.current),
        [registration],
    );
}
