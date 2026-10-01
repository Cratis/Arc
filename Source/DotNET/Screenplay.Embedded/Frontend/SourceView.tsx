// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { getSource } from './api';
import type { DocumentSelection } from './Hierarchy';
import { useRemote } from './useRemote';
import { Status } from './Status';

/** The Screenplay source of the selected document, exactly as it is embedded. */
export const SourceView = ({ projectId, documentId }: DocumentSelection) => {
    const source = useRemote(
        signal => getSource(projectId, documentId, signal),
        [projectId, documentId]);

    if (source.status === 'loading') return <Status kind='loading' message='Loading source…' />;
    if (source.status === 'error') return <Status kind='error' message={`Could not load the source. ${source.error.message}`} />;
    if (source.value.trim().length === 0) return <Status kind='empty' message='This document has no source.' />;

    return <pre className='source' aria-label='Screenplay source'>{source.value}</pre>;
};
