// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '@cratis/arc/queries';
import React from 'react';

/**
 * React context that provides the application-scoped {@link QueryInstanceCache}.
 *
 * All observable and regular query hooks read from this context so that queries with
 * identical type and arguments share a single cached instance and its last known result.
 *
 * The context is initialized with a default (empty) cache so that hooks used outside
 * of an `<Arc>` provider still function — they simply do not share instances across
 * component trees.
 */
export const QueryInstanceCacheContext = React.createContext<QueryInstanceCache>(new QueryInstanceCache());
