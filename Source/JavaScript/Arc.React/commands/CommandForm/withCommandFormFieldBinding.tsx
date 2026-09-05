// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import type { CommandFormFieldProps } from './CommandFormField';
import { CommandFormFieldWrapper } from './CommandFormFields';
import { useIsCommandFormFieldBound } from './commandFormFieldBindingContext';
import {
    markAsRuntimeBindingCommandFormField,
    type CommandFormMarked,
} from './commandFormMarkers';

/**
 * Gives a marked field the ability to bind itself when it is rendered behind an opaque custom component.
 * @param component The field implementation that consumes injected CommandForm field props.
 * @returns A component with the same public call signature and cross-package field markers.
 */
export function withCommandFormFieldBinding<TComponent>(
    component: TComponent,
): TComponent & CommandFormMarked {
    const Component = component as React.ComponentType<CommandFormFieldProps>;

    const RuntimeBindingField = (props: CommandFormFieldProps): React.ReactElement => {
        const isFrameworkBound = useIsCommandFormFieldBound();
        if (!isFrameworkBound) {
            const field = React.createElement(
                RuntimeBindingField,
                props,
            ) as React.ReactElement<CommandFormFieldProps>;
            return <CommandFormFieldWrapper field={field} />;
        }

        return <Component {...props} />;
    };

    const markedField = markAsRuntimeBindingCommandFormField(RuntimeBindingField);
    markedField.commandFormFieldName =
        Component.displayName || Component.name || 'CommandFormField';
    // SAFETY: The wrapper preserves the component's public call signature and only adds static markers.
    return markedField as unknown as TComponent & CommandFormMarked;
}
