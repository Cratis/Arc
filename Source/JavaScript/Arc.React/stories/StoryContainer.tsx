// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';

export interface StoryContainerProps {
    /**
     * The content to render within the container
     */
    children: React.ReactNode;
    
    /**
     * The size variant of the container
     * - 'sm': 600px max width
     * - 'md': 1200px max width (default)
     * - 'lg': 1400px max width
     * - 'full': no max width
     */
    size?: 'sm' | 'md' | 'lg' | 'full';
    
    /**
     * Whether to render the container as a card with background and border
     */
    asCard?: boolean;
    
    /**
     * Additional CSS classes to apply
     */
    className?: string;
}

/**
 * A container component for wrapping Storybook stories with consistent spacing and styling.
 *
 * The styling ships with the package and is imported once, from your `.storybook/preview`:
 *
 * ```ts
 * import '@cratis/arc.react/stories/styles.css';
 * ```
 *
 * Deliberately not side-effect imported from this file. That would put the stylesheet's path into the
 * emitted JavaScript, and a bare `tsc -b` - which is what builds this package when another TypeScript
 * project references it - copies no assets, so the import would resolve to nothing. One import in the
 * consumer is the price of the two builders emitting the same thing.
 *
 * Once loaded, the stylesheet is dark by default and adapts to light mode through CSS variables wherever
 * `data-theme="light"` is set on an ancestor.
 *
 * @example
 * ```tsx
 * export const MyStory: Story = {
 *   render: () => (
 *     <StoryContainer>
 *       <h1>My Component</h1>
 *       <MyComponent />
 *     </StoryContainer>
 *   ),
 * };
 * ```
 */
export const StoryContainer: React.FC<StoryContainerProps> = ({
    children,
    size = 'md',
    asCard = false,
    className = '',
}) => {
    const sizeClass = size === 'full' 
        ? 'story-container' 
        : size === 'sm' 
            ? 'story-container-sm' 
            : size === 'lg' 
                ? 'story-container-lg' 
                : 'story-container';
    
    const cardClass = asCard ? 'story-card' : '';
    const classes = [sizeClass, cardClass, className].filter(Boolean).join(' ');
    
    return <div className={classes}>{children}</div>;
};

/**
 * A section component for grouping related content within stories
 */
export const StorySection: React.FC<{ children: React.ReactNode; className?: string }> = ({
    children,
    className = '',
}) => {
    return <div className={`story-section ${className}`}>{children}</div>;
};

/**
 * A grid container for displaying multiple items in a responsive grid
 */
export const StoryGrid: React.FC<{ children: React.ReactNode; className?: string }> = ({
    children,
    className = '',
}) => {
    return <div className={`story-grid ${className}`}>{children}</div>;
};

/**
 * A visual divider between story sections
 */
export const StoryDivider: React.FC = () => {
    return <hr className="story-divider" />;
};

export type BadgeVariant = 'success' | 'warning' | 'error' | 'info';

/**
 * A status badge component for displaying colored labels
 */
export const StoryBadge: React.FC<{
    children: React.ReactNode;
    variant: BadgeVariant;
    className?: string;
}> = ({ children, variant, className = '' }) => {
    const variantClass = `story-badge-${variant}`;
    return <span className={`story-badge ${variantClass} ${className}`}>{children}</span>;
};
