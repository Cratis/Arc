// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { IQueryScope } from './IQueryScope.js';

/* eslint-disable @typescript-eslint/no-empty-function */
export const defaultQueryScopeContext: IQueryScope = new class extends IQueryScope {
    get parent() { return undefined; }
    get isPerforming() { return false; }
    addChildScope() { }
    notifyPerformingStarted() { }
    notifyPerformingCompleted() { }
}();
/* eslint-enable @typescript-eslint/no-empty-function */

export const QueryScopeContext = React.createContext<IQueryScope>(defaultQueryScopeContext);
