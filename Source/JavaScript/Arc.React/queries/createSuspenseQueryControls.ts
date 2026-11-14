// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Paging, type Sorting } from '@cratis/arc/queries';
import type { SetSorting } from './SetSorting.js';
import type { SetPage } from './SetPage.js';
import type { SetPageSize } from './SetPageSize.js';

/** Creates paging and sorting delegates with an optional reset before selecting a new resource. */
export function createSuspenseQueryControls(options: {
    paging: Paging;
    setSorting: (sorting: Sorting) => void;
    setPaging: (paging: Paging) => void;
    reset?: () => void;
}): [SetSorting, SetPage, SetPageSize] {
    return [
        async (sorting: Sorting) => {
            options.reset?.();
            options.setSorting(sorting);
        },
        async (page: number) => {
            options.reset?.();
            options.setPaging(new Paging(page, options.paging.pageSize));
        },
        async (pageSize: number) => {
            options.reset?.();
            options.setPaging(new Paging(options.paging.page, pageSize));
        },
    ];
}
