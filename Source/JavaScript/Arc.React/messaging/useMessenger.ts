// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { ArcContext } from '../ArcContext';
import { MessengerScopeContext } from './MessengerScopeContext';
import { IMessenger } from '@cratis/arc/messaging';

/**
 * React hook for accessing the nearest {@link IMessenger}.
 * @returns The nearest scoped messenger, or the Arc root messenger when no local scope is present.
 */
export function useMessenger(): IMessenger {
    return React.useContext(MessengerScopeContext) ?? React.useContext(ArcContext).messenger!;
}
