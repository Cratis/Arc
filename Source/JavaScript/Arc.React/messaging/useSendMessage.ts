// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo } from 'react';
import { useMessenger } from './useMessenger';

/**
 * Returns a stable function for publishing messages to the nearest {@link MessengerScope}
 * (or the Arc root messenger when no local scope is present).
 *
 * Use {@link useOnMessage} for subscribing inside components.
 *
 * @returns A function that publishes the supplied message to the resolved messenger.
 */
export const useSendMessage = (): (<TMessage extends object>(message: TMessage) => void) => {
    const messenger = useMessenger();
    return useMemo(() => messenger.publish.bind(messenger), [messenger]);
};
