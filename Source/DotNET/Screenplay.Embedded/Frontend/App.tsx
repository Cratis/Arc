// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useState } from 'react';
import { getHierarchy } from './api';
import { Hierarchy, rootDocumentOf, type DocumentSelection } from './Hierarchy';
import { CanvasView } from './CanvasView';
import { SourceView } from './SourceView';
import { Status } from './Status';
import { useRemote } from './useRemote';
import cratisLogo from './assets/cratis.svg';

type Tab = 'canvas' | 'source';

const tabs: { id: Tab; label: string }[] = [
    { id: 'canvas', label: 'Event model' },
    { id: 'source', label: 'Source' }
];

/** The embedded event-model explorer: the documents on the left, the selected one on the right. */
export const App = () => {
    const hierarchy = useRemote(getHierarchy, []);
    const [selection, setSelection] = useState<DocumentSelection>();
    const [tab, setTab] = useState<Tab>('canvas');

    const projects = hierarchy.status === 'loaded' ? hierarchy.value : [];

    useEffect(() => {
        if (selection) return;
        const project = projects.find(candidate => candidate.documents.length > 0);
        const root = project && rootDocumentOf(project);
        if (project && root) setSelection(current => current ?? { projectId: project.id, documentId: root.id });
    }, [projects, selection]);

    if (hierarchy.status === 'loading') return <Status kind='loading' message='Loading event model documents…' />;
    if (hierarchy.status === 'error') {
        return <Status kind='error' message={`Could not load the event model documents. ${hierarchy.error.message}`} />;
    }
    if (projects.length === 0) return <Status kind='empty' message='No projects with embedded event models were found.' />;

    return (
        <div className='app'>
            <aside className='sidebar'>
                <a className='sidebar-brand' href='https://cratis.io' target='_blank' rel='noopener noreferrer' aria-label='Visit Cratis (opens in a new tab)'>
                    <img src={cratisLogo} alt='Cratis' />
                </a>
                <Hierarchy projects={projects} selection={selection} onSelect={setSelection} />
            </aside>
            <main className='document'>
                {!selection
                    ? <Status kind='empty' message='Select a document to view it.' />
                    : (
                        <>
                            <div className='tabs' role='tablist' aria-label='Document views'>
                                {tabs.map(({ id, label }) => (
                                    <button
                                        key={id}
                                        type='button'
                                        role='tab'
                                        id={`tab-${id}`}
                                        aria-selected={tab === id}
                                        aria-controls={`panel-${id}`}
                                        className={`tab${tab === id ? ' selected' : ''}`}
                                        onClick={() => setTab(id)}>
                                        {label}
                                    </button>
                                ))}
                            </div>
                            <div className='panel' role='tabpanel' id={`panel-${tab}`} aria-labelledby={`tab-${tab}`}>
                                {tab === 'canvas'
                                    ? <CanvasView key={`canvas-${selection.projectId}-${selection.documentId}`} {...selection} />
                                    : <SourceView key={`source-${selection.projectId}-${selection.documentId}`} {...selection} />}
                            </div>
                        </>
                    )}
            </main>
        </div>
    );
};
