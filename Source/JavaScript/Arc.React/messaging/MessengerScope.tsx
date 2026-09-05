// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useEffect, useMemo } from 'react';
import { Messenger } from '@cratis/arc/messaging';
import { useMessenger } from './useMessenger';
import { MessengerScopeContext } from './MessengerScopeContext';

export interface IMessengerScopeProps {
    children?: JSX.Element | JSX.Element[];

    /**
     * Whether messages published in this scope should bubble to parent scope.
     * Defaults to false.
     */
    bubbleToParent?: boolean;

    /**
     * Whether messages in this scope should trickle down to child scopes.
     * Defaults to true.
     */
    trickleDownToChildren?: boolean;
}

export const MessengerScope = (props: IMessengerScopeProps) => {
    const parent = useMessenger();
    const messenger = useMemo(() => new Messenger(parent instanceof Messenger ? parent : undefined), [parent]);

    useEffect(() => {
        messenger.bubbleToParent = props.bubbleToParent ?? false;
        messenger.trickleDownToChildren = props.trickleDownToChildren ?? true;
    }, [messenger, props.bubbleToParent, props.trickleDownToChildren]);

    useEffect(() => () => messenger.dispose(), [messenger]);

    return (
        <MessengerScopeContext.Provider value={messenger}>
            {props.children}
        </MessengerScopeContext.Provider>
    );
};
