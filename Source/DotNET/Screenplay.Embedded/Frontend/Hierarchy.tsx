// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { DocumentKind, HierarchyDocument, HierarchyProject } from './api';

/** Identifies one document within one project. */
export interface DocumentSelection {
    projectId: string;
    documentId: string;
}

export interface HierarchyProps {
    projects: HierarchyProject[];
    selection?: DocumentSelection;
    onSelect: (selection: DocumentSelection) => void;
}

const kinds: Record<DocumentKind, { icon: string; label: string }> = {
    assembly: { icon: 'pi pi-box', label: 'Assembly' },
    module: { icon: 'pi pi-sitemap', label: 'Module' },
    feature: { icon: 'pi pi-bolt', label: 'Feature' }
};

const describe = (kind: DocumentKind) => kinds[kind] ?? { icon: 'pi pi-file', label: kind };

/** Picks the document a project opens on - its root, the assembly document when there is one. */
export const rootDocumentOf = (project: HierarchyProject): HierarchyDocument | undefined => {
    const roots = project.documents.filter(document => !document.parentId);
    return roots.find(document => document.kind === 'assembly') ?? roots[0];
};

const childrenOf = (documents: HierarchyDocument[], parentId: string | null) =>
    documents.filter(document => (document.parentId ?? null) === parentId);

interface DocumentNodesProps {
    project: HierarchyProject;
    parentId: string | null;
    level: number;
    selection?: DocumentSelection;
    onSelect: (selection: DocumentSelection) => void;
}

const DocumentNodes = ({ project, parentId, level, selection, onSelect }: DocumentNodesProps) => {
    const documents = childrenOf(project.documents, parentId);
    if (documents.length === 0) return null;

    return (
        <ul className='hierarchy-nodes'>
            {documents.map(document => {
                const { icon, label } = describe(document.kind);
                const selected = selection?.projectId === project.id && selection?.documentId === document.id;
                return (
                    <li key={document.id} role='none'>
                        <button
                            type='button'
                            role='treeitem'
                            aria-level={level}
                            aria-selected={selected}
                            className={`hierarchy-item${selected ? ' selected' : ''}`}
                            title={document.namespace}
                            onClick={() => onSelect({ projectId: project.id, documentId: document.id })}>
                            <span className={icon} aria-hidden='true' />
                            <span className='hierarchy-title'>{document.title}</span>{' '}
                            <span className='hierarchy-kind'>{label}</span>
                        </button>
                        <DocumentNodes
                            project={project}
                            parentId={document.id}
                            level={level + 1}
                            selection={selection}
                            onSelect={onSelect} />
                    </li>
                );
            })}
        </ul>
    );
};

/** The navigation over every project and the documents embedded in it. */
export const Hierarchy = ({ projects, selection, onSelect }: HierarchyProps) => (
    <nav className='hierarchy' aria-label='Event model documents'>
        {projects.map(project => (
            <section key={project.id} className='hierarchy-project'>
                <h2 id={`project-${project.id}`} className='hierarchy-project-name'>{project.name}</h2>
                {project.documents.length === 0
                    ? <p className='empty'>No documents are embedded in this project.</p>
                    : (
                        <div role='tree' aria-labelledby={`project-${project.id}`}>
                            <DocumentNodes
                                project={project}
                                parentId={null}
                                level={1}
                                selection={selection}
                                onSelect={onSelect} />
                        </div>
                    )}
            </section>
        ))}
    </nav>
);
