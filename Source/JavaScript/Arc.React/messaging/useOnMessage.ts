// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from '@cratis/fundamentals';
import { useEffect, useRef } from 'react';
import { useMessenger } from './useMessenger';

/**
 * Subscribes to messages of type {@link TMessage} from the nearest {@link MessengerScope}
 * (or the Arc root messenger) for the lifetime of the calling component.
 * The subscription is automatically cleaned up on unmount.
 *
 * The callback does not need to be wrapped in `useCallback` — a ref is used internally
 * so stale-closure issues are avoided without forcing the caller to memoize.
 *
 * @param type Constructor of the message type to subscribe to.
 * @param callback Invoked whenever a matching message is published and the filter (if any) accepts it.
 * @param filter Optional predicate invoked with the actual typed message; only when it returns `true`
 *               will {@link callback} be invoked. Useful for narrowing on message content
 *               without re-subscribing.
 */
export const useOnMessage = <TMessage extends object>(
    type: Constructor<TMessage>,
    callback: (message: TMessage) => void,
    filter?: (message: TMessage) => boolean
): void => {
    const messenger = useMessenger();
    const callbackRef = useRef(callback);
    callbackRef.current = callback;
    const filterRef = useRef(filter);
    filterRef.current = filter;

    useEffect(() => {
        const subscription = messenger.subscribe(type, message => {
            if (filterRef.current && !filterRef.current(message)) {
                return;
            }
            callbackRef.current(message);
        });
        return () => subscription.unsubscribe();
    }, [messenger, type]);
};
