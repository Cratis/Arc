// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { QueryScopeContext } from './QueryScope';

/**
 * React hook for accessing the current {@link IQueryScope} from the context.
 * @returns The current {@link IQueryScope} instance.
 */
export function useQueryScope() {
    return React.useContext(QueryScopeContext);
}
