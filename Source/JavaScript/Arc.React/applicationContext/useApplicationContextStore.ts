// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useContext } from 'react';
import { ArcContext } from '../ArcContext.js';
import { ApplicationContext } from './ApplicationContext.js';

/**
 * Gets the {@link ApplicationContext} store provided by `<Arc>`.
 * @returns The store, or undefined when not rendered within `<Arc>`.
 */
export function useApplicationContextStore(): ApplicationContext | undefined {
    return useContext(ArcContext).applicationContext;
}
