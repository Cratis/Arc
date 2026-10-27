// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';
import { useSliceContext } from '../useSliceContext.js';

/**
 * A shared component that knows nothing about where it is rendered, like a report button.
 */
export const ReportButton = (props: { onRender?: (location: SliceLocation) => void }) => {
    const location = useSliceContext();
    props.onRender?.(location);
    return <span data-testid="location">{location.toString()}</span>;
};
