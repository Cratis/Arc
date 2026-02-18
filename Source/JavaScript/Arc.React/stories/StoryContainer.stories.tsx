// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { Meta, StoryObj } from '@storybook/react';
import { StoryContainer, StorySection, StoryGrid, StoryDivider, StoryBadge } from './StoryContainer';

const meta: Meta<typeof StoryContainer> = {
    title: 'Stories/StoryContainer',
    component: StoryContainer,
    parameters: {
        docs: {
            description: {
                component: 'Container components for wrapping Storybook stories with consistent styling that automatically adapts to dark/light mode.',
            },
        },
    },
};

export default meta;
type Story = StoryObj<typeof StoryContainer>;

export const BasicUsage: Story = {
    render: () => (
        <StoryContainer>
            <h1>Story Container Example</h1>
            <p>
                This is a basic story container with default medium size (1200px max width).
                It provides consistent padding and centering.
            </p>
            <p>
                The container automatically adapts to both dark and light modes using CSS variables.
                Try switching the background in the toolbar above!
            </p>
        </StoryContainer>
    ),
};

export const SizeVariants: Story = {
    render: () => (
        <>
            <StoryContainer size="sm">
                <h2>Small Container</h2>
                <p>This container has a max-width of 600px, perfect for focused content.</p>
            </StoryContainer>
            
            <StoryDivider />
            
            <StoryContainer size="md">
                <h2>Medium Container (Default)</h2>
                <p>This container has a max-width of 1200px, suitable for most stories.</p>
            </StoryContainer>
            
            <StoryDivider />
            
            <StoryContainer size="lg">
                <h2>Large Container</h2>
                <p>This container has a max-width of 1400px, for wider layouts.</p>
            </StoryContainer>
        </>
    ),
};

export const CardStyle: Story = {
    render: () => (
        <StoryContainer asCard>
            <h2>Card Container</h2>
            <p>
                This container is rendered as a card with a background, border, and shadow.
                Perfect for highlighting content or creating distinct sections.
            </p>
            <p>Notice how the card adapts to the current theme.</p>
        </StoryContainer>
    ),
};

export const WithSections: Story = {
    render: () => (
        <StoryContainer>
            <h1>Using Sections</h1>
            
            <StorySection>
                <h2>First Section</h2>
                <p>Sections provide consistent vertical spacing between groups of content.</p>
            </StorySection>
            
            <StorySection>
                <h2>Second Section</h2>
                <p>Each section automatically has margin-bottom spacing.</p>
            </StorySection>
            
            <StorySection>
                <h2>Third Section</h2>
                <p>The last section has no bottom margin to prevent extra space.</p>
            </StorySection>
        </StoryContainer>
    ),
};

export const WithGrid: Story = {
    render: () => (
        <StoryContainer>
            <h1>Grid Layout</h1>
            <p>Use StoryGrid for responsive grid layouts:</p>
            
            <StoryGrid>
                <div className="story-card">
                    <h3>Card 1</h3>
                    <p>Grid items automatically wrap based on available space.</p>
                </div>
                <div className="story-card">
                    <h3>Card 2</h3>
                    <p>Minimum width of 300px per item.</p>
                </div>
                <div className="story-card">
                    <h3>Card 3</h3>
                    <p>Consistent gap between items.</p>
                </div>
                <div className="story-card">
                    <h3>Card 4</h3>
                    <p>Adapts to dark/light mode.</p>
                </div>
            </StoryGrid>
        </StoryContainer>
    ),
};

export const WithBadges: Story = {
    render: () => (
        <StoryContainer>
            <h1>Status Badges</h1>
            <p>Use badges to display status or categorical information:</p>
            
            <StorySection>
                <h3>Badge Variants</h3>
                <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap', marginTop: '1rem' }}>
                    <StoryBadge variant="success">Success</StoryBadge>
                    <StoryBadge variant="warning">Warning</StoryBadge>
                    <StoryBadge variant="error">Error</StoryBadge>
                    <StoryBadge variant="info">Info</StoryBadge>
                </div>
            </StorySection>
            
            <StorySection>
                <h3>In Context</h3>
                <p>
                    Build Status: <StoryBadge variant="success">Passing</StoryBadge>
                </p>
                <p>
                    Deployment: <StoryBadge variant="warning">Pending</StoryBadge>
                </p>
                <p>
                    Tests: <StoryBadge variant="error">5 Failed</StoryBadge>
                </p>
                <p>
                    Coverage: <StoryBadge variant="info">87%</StoryBadge>
                </p>
            </StorySection>
        </StoryContainer>
    ),
};

export const FormExample: Story = {
    render: () => (
        <StoryContainer size="sm" asCard>
            <h2>Login Form</h2>
            <p>This demonstrates how the CSS styles automatically apply to form elements:</p>
            
            <form style={{ marginTop: '1.5rem' }} onSubmit={(e) => e.preventDefault()}>
                <div style={{ marginBottom: '1rem' }}>
                    <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 500 }}>
                        Email
                    </label>
                    <input
                        type="email"
                        placeholder="you@example.com"
                        style={{ width: '100%', display: 'block' }}
                    />
                </div>
                
                <div style={{ marginBottom: '1rem' }}>
                    <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 500 }}>
                        Password
                    </label>
                    <input
                        type="password"
                        placeholder="••••••••"
                        style={{ width: '100%', display: 'block' }}
                    />
                </div>
                
                <button type="submit" style={{ width: '100%', marginTop: '0.5rem' }}>
                    Sign In
                </button>
            </form>
        </StoryContainer>
    ),
};

export const Typography: Story = {
    render: () => (
        <StoryContainer>
            <h1>Typography Showcase</h1>
            <p>All typography automatically adapts to the current theme.</p>
            
            <StoryDivider />
            
            <StorySection>
                <h1>Heading 1</h1>
                <h2>Heading 2</h2>
                <h3>Heading 3</h3>
                <h4>Heading 4</h4>
                <h5>Heading 5</h5>
                <h6>Heading 6</h6>
            </StorySection>
            
            <StorySection>
                <p>
                    This is a paragraph with <a href="#">a link</a> and some <code>inline code</code>.
                    The typography uses a modern font stack optimized for readability.
                </p>
                
                <pre><code>{`function example() {
  return "This is a code block";
}`}</code></pre>
            </StorySection>
            
            <StorySection>
                <ul>
                    <li>Unordered list item 1</li>
                    <li>Unordered list item 2</li>
                    <li>Unordered list item 3</li>
                </ul>
                
                <ol>
                    <li>Ordered list item 1</li>
                    <li>Ordered list item 2</li>
                    <li>Ordered list item 3</li>
                </ol>
            </StorySection>
        </StoryContainer>
    ),
};
