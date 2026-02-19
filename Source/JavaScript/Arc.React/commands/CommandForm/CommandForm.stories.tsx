// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useState } from 'react';
import { Meta, StoryObj } from '@storybook/react';
import { CommandForm } from './CommandForm';
import { UserRegistrationCommand } from './UserRegistrationCommand';
import { 
    InputTextField, 
    NumberField, 
    TextAreaField, 
    CheckboxField, 
    RangeField,
    SelectField 
} from './fields';
import { Command, CommandValidator } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { StoryContainer, StoryBadge } from '../../stories';
import type { FieldDecoratorProps, ErrorDisplayProps, TooltipWrapperProps } from './CommandFormContext';
import '@cratis/arc/validation';

const meta: Meta<typeof CommandForm> = {
    title: 'CommandForm/CommandForm',
    component: CommandForm,
};

export default meta;
type Story = StoryObj<typeof CommandForm>;

// Simple command for the default story with validation
class SimpleCommand extends Command {
    readonly route: string = '/api/simple';
    readonly validation: SimpleCommandValidator = new SimpleCommandValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('name', String),
        new PropertyDescriptor('email', String),
    ];

    name = '';
    email = '';

    constructor() {
        super(Object, false);
    }

    get requestParameters(): string[] {
        return [];
    }

    get properties(): string[] {
        return ['name', 'email'];
    }
}

class SimpleCommandValidator extends CommandValidator<SimpleCommand> {
    constructor() {
        super();
        this.ruleFor(c => c.name).notEmpty().minLength(3);
        this.ruleFor(c => c.email).notEmpty().emailAddress();
    }
}

const roleOptions = [
    { id: 'user', name: 'User' },
    { id: 'admin', name: 'Administrator' },
    { id: 'moderator', name: 'Moderator' }
];

export const Default: Story = {
    render: () => {
        const [validationState, setValidationState] = useState<{
            errors: Record<string, string>;
            canSubmit: boolean;
        }>({ errors: {}, canSubmit: false });

        return (
            <StoryContainer size="sm" asCard>
                <h2>Simple Command Form with Validation</h2>
                <p>
                    This form demonstrates validation on blur. Fields are validated when you leave them.
                </p>
                <CommandForm<SimpleCommand>
                    command={SimpleCommand}
                    initialValues={{
                        name: '',
                        email: '',
                    }}
                    onFieldChange={async (command, fieldName) => {
                        // Validate on blur pattern
                        const result = await command.validate();
                        
                        if (!result.isValid) {
                            const fieldError = result.validationResults.find(
                                v => v.members.includes(fieldName)
                            );
                            
                            if (fieldError) {
                                setValidationState(prev => ({
                                    errors: { ...prev.errors, [fieldName]: fieldError.message },
                                    canSubmit: false
                                }));
                            }
                        } else {
                            setValidationState(prev => {
                                const { [fieldName]: removed, ...rest } = prev.errors;
                                return { errors: rest, canSubmit: true };
                            });
                        }
                    }}
                >
                    <InputTextField<SimpleCommand> 
                        value={c => c.name} 
                        title="Name"
                        placeholder="Enter your name (min 3 chars)" 
                    />
                    {validationState.errors.name && (
                        <div style={{ color: 'var(--color-error)', fontSize: '0.875rem', marginTop: '0.25rem', marginBottom: '1rem' }}>
                            {validationState.errors.name}
                        </div>
                    )}
                    
                    <InputTextField<SimpleCommand> 
                        value={c => c.email} 
                        title="Email"
                        type="email" 
                        placeholder="Enter your email" 
                    />
                    {validationState.errors.email && (
                        <div style={{ color: 'var(--color-error)', fontSize: '0.875rem', marginTop: '0.25rem', marginBottom: '1rem' }}>
                            {validationState.errors.email}
                        </div>
                    )}

                    <div style={{ marginTop: '1.5rem', display: 'flex', gap: '0.5rem', alignItems: 'center', flexWrap: 'wrap' }}>
                        <button 
                            type="submit" 
                            disabled={!validationState.canSubmit}
                        >
                            Submit
                        </button>
                        {!validationState.canSubmit && Object.keys(validationState.errors).length > 0 && (
                            <StoryBadge variant="warning">Please fix validation errors</StoryBadge>
                        )}
                    </div>
                </CommandForm>
            </StoryContainer>
        );
    }
};

export const UserRegistration: Story = {
    render: () => {
        const [result, setResult] = useState<string>('');
        const [canSubmit, setCanSubmit] = useState(false);
        const [validationSummary, setValidationSummary] = useState<string[]>([]);

        const handleSubmit = () => {
            setResult('Form submitted successfully!');
        };

        return (
            <StoryContainer size="sm" asCard>
                <h1>User Registration Form</h1>
                <p>
                    This form validates progressively as you type. The submit button is enabled only when all validation passes.
                </p>
                               
                <CommandForm<UserRegistrationCommand>
                    command={UserRegistrationCommand}
                    initialValues={{
                        username: '',
                        email: '',
                        password: '',
                        confirmPassword: '',
                        age: 18,
                        bio: '',
                        favoriteColor: '#3b82f6',
                        birthDate: '',
                        agreeToTerms: false,
                        experienceLevel: 50,
                        role: ''
                    }}
                    onFieldChange={async (command, fieldName, oldValue, newValue) => {
                        console.log(`Field ${fieldName} changed from`, oldValue, 'to', newValue);
                        
                        // Progressive validation - validate whenever fields change
                        const validationResult = await command.validate();
                        
                        if (!validationResult.isValid) {
                            setValidationSummary(validationResult.validationResults.map(v => v.message));
                            setCanSubmit(false);
                        } else {
                            setValidationSummary([]);
                            setCanSubmit(true);
                        }
                    }}
                >
                    <h3>Account Information</h3>
                    <InputTextField<UserRegistrationCommand> 
                        value={c => c.username} 
                        title="Username"
                        placeholder="Enter username" 
                    />
                    
                    <InputTextField<UserRegistrationCommand> 
                        value={c => c.email} 
                        title="Email Address"
                        type="email" 
                        placeholder="Enter email" 
                    />
                    
                    <InputTextField<UserRegistrationCommand> 
                        value={c => c.password} 
                        title="Password"
                        type="password" 
                        placeholder="Enter password" 
                    />
                    
                    <InputTextField<UserRegistrationCommand> 
                        value={c => c.confirmPassword} 
                        title="Confirm Password"
                        type="password" 
                        placeholder="Confirm password" 
                    />

                    <h3 style={{ marginTop: 'var(--space-2xl)', marginBottom: 0 }}>Personal Information</h3>
                    
                    <NumberField<UserRegistrationCommand> 
                        value={c => c.age} 
                        title="Age"
                        placeholder="Enter age" 
                        min={13} 
                        max={120} 
                    />
                    
                    <InputTextField<UserRegistrationCommand> 
                        value={c => c.birthDate} 
                        title="Birth Date"
                        type="date" 
                        placeholder="Select birth date" 
                    />
                    
                    <TextAreaField<UserRegistrationCommand> 
                        value={c => c.bio} 
                        title="Bio"
                        placeholder="Tell us about yourself" 
                        rows={4} 
                        required={false} 
                    />
                    
                    <InputTextField<UserRegistrationCommand> 
                        value={c => c.favoriteColor} 
                        title="Favorite Color"
                        type="color" 
                    />

                    <h3 style={{ marginTop: 'var(--space-2xl)', marginBottom: 0 }}>Preferences</h3>
                    
                    <SelectField<UserRegistrationCommand>
                        value={c => c.role}
                        title="Role"
                        options={roleOptions} 
                        optionIdField="id" 
                        optionLabelField="name"
                        placeholder="Select a role"
                    />
                    
                    <RangeField<UserRegistrationCommand> 
                        value={c => c.experienceLevel} 
                        title="Experience Level"
                        min={0} 
                        max={100} 
                        step={10} 
                    />
                    
                    <CheckboxField<UserRegistrationCommand> 
                        value={c => c.agreeToTerms} 
                        label="I agree to the terms and conditions" 
                    />
                </CommandForm>

                {validationSummary.length > 0 && (
                    <div className="story-card" style={{ 
                        backgroundColor: 'rgba(245, 158, 11, 0.1)', 
                        borderColor: 'var(--color-warning)',
                        marginBottom: 'var(--space-lg)'
                    }}>
                        <strong style={{ color: 'var(--color-warning)' }}>Validation Issues:</strong>
                        <ul style={{ marginTop: 'var(--space-sm)', marginBottom: 0 }}>
                            {validationSummary.map((error, index) => (
                                <li key={index} style={{ color: 'var(--color-warning)' }}>{error}</li>
                            ))}
                        </ul>
                    </div>
                )}

                <div style={{ display: 'flex', gap: 'var(--space-md)', marginTop: 'var(--space-xl)', alignItems: 'center', flexWrap: 'wrap' }}>
                    <button 
                        onClick={handleSubmit} 
                        disabled={!canSubmit}
                        style={{ backgroundColor: canSubmit ? 'var(--color-success)' : undefined }}
                    >
                        Submit
                    </button>
                    <button 
                        onClick={() => setResult('')}
                        style={{ backgroundColor: 'var(--color-text-muted)' }}
                    >
                        Cancel
                    </button>
                    {!canSubmit && (
                        <StoryBadge variant="warning">Complete required fields with valid data</StoryBadge>
                    )}
                </div>

                {result && (
                    <div className="story-card" style={{ 
                        backgroundColor: 'rgba(34, 197, 94, 0.1)',
                        borderColor: 'var(--color-success)',
                        marginTop: 'var(--space-lg)'
                    }}>
                        <p style={{ color: 'var(--color-success)', fontWeight: 600, margin: 0 }}>{result}</p>
                    </div>
                )}
            </StoryContainer>
        );
    }
};

export const CustomTitles: Story = {
    render: () => {
        return (
            <StoryContainer size="sm" asCard>
                <h2>Custom Titles</h2>
                <p>
                    This form shows how to disable built-in titles and use custom title rendering.
                </p>
                <CommandForm<SimpleCommand>
                    command={SimpleCommand}
                    showTitles={false}
                >
                    <div style={{ marginBottom: '1rem' }}>
                        <div style={{ 
                            fontSize: '0.75rem', 
                            textTransform: 'uppercase', 
                            letterSpacing: '0.05em', 
                            marginBottom: '0.5rem',
                            color: 'var(--color-text-secondary)',
                            fontWeight: 600
                        }}>
                            Full Name *
                        </div>
                        <InputTextField<SimpleCommand> 
                            value={c => c.name} 
                            placeholder="Enter your full name" 
                        />
                    </div>

                    <div style={{ marginBottom: '1rem' }}>
                        <div style={{ 
                            fontSize: '0.875rem', 
                            marginBottom: '0.5rem',
                            color: 'var(--color-primary)',
                            fontWeight: 700
                        }}>
                            📧 Email Address
                        </div>
                        <InputTextField<SimpleCommand> 
                            value={c => c.email} 
                            type="email" 
                            placeholder="your.email@example.com" 
                        />
                    </div>

                    <button type="submit">Submit</button>
                </CommandForm>
            </StoryContainer>
        );
    }
};

export const CustomErrorRendering: Story = {
    render: () => {
        const [errors, setErrors] = useState<Record<string, string>>({});

        return (
            <StoryContainer size="sm" asCard>
                <h2>Custom Error Rendering</h2>
                <p>
                    This form shows how to disable built-in error messages and render custom ones.
                </p>
                <CommandForm<SimpleCommand>
                    command={SimpleCommand}
                    showErrors={false}
                    onFieldChange={async (command, fieldName) => {
                        const result = await command.validate();
                        
                        if (!result.isValid) {
                            const fieldError = result.validationResults.find(
                                v => v.members.includes(fieldName)
                            );
                            
                            if (fieldError) {
                                setErrors(prev => ({ ...prev, [fieldName]: fieldError.message }));
                            }
                        } else {
                            setErrors(prev => {
                                const { [fieldName]: removed, ...rest } = prev;
                                return rest;
                            });
                        }
                    }}
                >
                    <InputTextField<SimpleCommand> 
                        value={c => c.name} 
                        title="Name"
                        placeholder="Enter your name (min 3 chars)" 
                    />
                    {errors.name && (
                        <div style={{ 
                            backgroundColor: 'rgba(239, 68, 68, 0.1)',
                            border: '1px solid var(--color-error)',
                            borderRadius: 'var(--radius-md)',
                            padding: '0.75rem',
                            marginTop: '0.5rem',
                            marginBottom: '1rem',
                            display: 'flex',
                            alignItems: 'center',
                            gap: '0.5rem'
                        }}>
                            <span style={{ fontSize: '1.25rem' }}>⚠️</span>
                            <div>
                                <strong style={{ color: 'var(--color-error)' }}>Validation Error</strong>
                                <div style={{ fontSize: '0.875rem', marginTop: '0.25rem', color: 'var(--color-text)' }}>
                                    {errors.name}
                                </div>
                            </div>
                        </div>
                    )}
                    
                    <InputTextField<SimpleCommand> 
                        value={c => c.email} 
                        title="Email"
                        type="email" 
                        placeholder="Enter your email" 
                    />
                    {errors.email && (
                        <div style={{ 
                            backgroundColor: 'rgba(239, 68, 68, 0.1)',
                            border: '1px solid var(--color-error)',
                            borderRadius: 'var(--radius-md)',
                            padding: '0.75rem',
                            marginTop: '0.5rem',
                            marginBottom: '1rem',
                            display: 'flex',
                            alignItems: 'center',
                            gap: '0.5rem'
                        }}>
                            <span style={{ fontSize: '1.25rem' }}>⚠️</span>
                            <div>
                                <strong style={{ color: 'var(--color-error)' }}>Validation Error</strong>
                                <div style={{ fontSize: '0.875rem', marginTop: '0.25rem', color: 'var(--color-text)' }}>
                                    {errors.email}
                                </div>
                            </div>
                        </div>
                    )}

                    <button type="submit">Submit</button>
                </CommandForm>
            </StoryContainer>
        );
    }
};

export const CustomFieldContainer: Story = {
    render: () => {
        const CustomContainer: React.FC<import('./CommandFormContext').FieldContainerProps> = ({ title, errorMessage, children }) => {
            return (
                <div style={{ 
                    marginBottom: '1.5rem',
                    padding: '1rem',
                    border: `2px solid ${errorMessage ? 'var(--color-error)' : 'var(--color-border)'}`,
                    borderRadius: 'var(--radius-lg)',
                    backgroundColor: errorMessage ? 'rgba(239, 68, 68, 0.05)' : 'var(--color-background-secondary)',
                    transition: 'all 0.2s ease'
                }}>
                    {title && (
                        <div style={{ 
                            fontSize: '0.875rem',
                            fontWeight: 600,
                            color: errorMessage ? 'var(--color-error)' : 'var(--color-text)',
                            marginBottom: '0.75rem',
                            display: 'flex',
                            alignItems: 'center',
                            gap: '0.5rem'
                        }}>
                            {errorMessage && <span>❌</span>}
                            {!errorMessage && <span>✓</span>}
                            {title}
                        </div>
                    )}
                    {children}
                    {errorMessage && (
                        <div style={{ 
                            marginTop: '0.5rem',
                            fontSize: '0.875rem',
                            color: 'var(--color-error)',
                            fontWeight: 500
                        }}>
                            {errorMessage}
                        </div>
                    )}
                </div>
            );
        };

        return (
            <StoryContainer size="sm" asCard>
                <h2>Custom Field Container</h2>
                <p>
                    This form shows how to use a custom component for rendering field containers.
                </p>
                <CommandForm<SimpleCommand>
                    command={SimpleCommand}
                    fieldContainerComponent={CustomContainer}
                >
                    <InputTextField<SimpleCommand> 
                        value={c => c.name} 
                        title="Name"
                        placeholder="Enter your name (min 3 chars)" 
                    />
                    
                    <InputTextField<SimpleCommand> 
                        value={c => c.email} 
                        title="Email"
                        type="email" 
                        placeholder="Enter your email" 
                    />

                    <button type="submit">Submit</button>
                </CommandForm>
            </StoryContainer>
        );
    }
};

export const CustomRenderers: Story = {
    render: () => {
        // Custom Field Decorator - wraps fields with icons and descriptions
        const CustomFieldDecorator = ({ icon, description, children }: FieldDecoratorProps) => {
            if (!icon && !description) {
                return <>{children}</>;
            }

            return (
                <div style={{ position: 'relative', display: 'flex', alignItems: 'stretch' }}>
                    {icon && (
                        <div style={{
                            display: 'flex',
                            alignItems: 'center',
                            padding: '0.75rem',
                            background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                            color: 'white',
                            borderRadius: '0.5rem 0 0 0.5rem',
                            fontWeight: 600
                        }}>
                            {icon}
                        </div>
                    )}
                    <div 
                        style={{ flex: 1 }}
                        title={description}
                    >
                        {children}
                    </div>
                </div>
            );
        };

        // Custom Error Display - shows validation errors with icons
        const CustomErrorDisplay = ({ errors, fieldName }: ErrorDisplayProps) => (
            <div style={{
                marginTop: '0.5rem',
                display: 'flex',
                flexDirection: 'column',
                gap: '0.25rem'
            }}>
                {errors.map((error, idx) => (
                    <div
                        key={idx}
                        style={{
                            display: 'flex',
                            alignItems: 'center',
                            gap: '0.5rem',
                            padding: '0.5rem',
                            backgroundColor: '#fef2f2',
                            border: '1px solid #fecaca',
                            borderRadius: '0.375rem',
                            fontSize: '0.875rem',
                            color: '#dc2626',
                            animation: 'slideIn 0.2s ease-out'
                        }}
                        role="alert"
                    >
                        <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
                            <path fillRule="evenodd" d="M8 16A8 8 0 1 0 8 0a8 8 0 0 0 0 16zM7 5a1 1 0 1 1 2 0v4a1 1 0 1 1-2 0V5zm1 7a1 1 0 1 0 0-2 1 1 0 0 0 0 2z" />
                        </svg>
                        <span><strong>{fieldName}:</strong> {error}</span>
                    </div>
                ))}
            </div>
        );

        // Custom Tooltip Wrapper - shows description as styled tooltip
        const CustomTooltip = ({ description, children }: TooltipWrapperProps) => {
            const [showTooltip, setShowTooltip] = useState(false);
            
            return (
                <div 
                    style={{ position: 'relative', display: 'inline-block', width: '100%' }}
                    onMouseEnter={() => setShowTooltip(true)}
                    onMouseLeave={() => setShowTooltip(false)}
                >
                    {children}
                    {showTooltip && (
                        <div style={{
                            position: 'absolute',
                            top: '100%',
                            left: '0',
                            marginTop: '0.5rem',
                            padding: '0.5rem 0.75rem',
                            backgroundColor: '#1e293b',
                            color: 'white',
                            borderRadius: '0.375rem',
                            fontSize: '0.875rem',
                            zIndex: 1000,
                            maxWidth: '300px',
                            boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)',
                            animation: 'fadeIn 0.15s ease-in'
                        }}>
                            {description}
                            <div style={{
                                position: 'absolute',
                                bottom: '100%',
                                left: '1rem',
                                width: 0,
                                height: 0,
                                borderLeft: '6px solid transparent',
                                borderRight: '6px solid transparent',
                                borderBottom: '6px solid #1e293b'
                            }} />
                        </div>
                    )}
                </div>
            );
        };

        return (
            <StoryContainer size="md" asCard>
                <style>{`
                    @keyframes slideIn {
                        from {
                            opacity: 0;
                            transform: translateY(-0.5rem);
                        }
                        to {
                            opacity: 1;
                            transform: translateY(0);
                        }
                    }
                    @keyframes fadeIn {
                        from { opacity: 0; }
                        to { opacity: 1; }
                    }
                `}</style>
                
                <h2>Custom Renderers</h2>
                <p>
                    This form demonstrates all the customization options: custom field decorators, 
                    error displays, tooltips, and CSS classes. The form is completely framework-agnostic 
                    and can be styled to match any design system.
                </p>

                <div style={{
                    backgroundColor: '#f8fafc',
                    padding: '1rem',
                    borderRadius: '0.5rem',
                    marginBottom: '1.5rem',
                    border: '1px solid #e2e8f0'
                }}>
                    <h4 style={{ marginTop: 0, fontSize: '0.875rem', fontWeight: 600, color: '#64748b' }}>
                        ℹ️ Features Demonstrated
                    </h4>
                    <ul style={{ marginBottom: 0, fontSize: '0.875rem', color: '#475569' }}>
                        <li><strong>Custom Field Decorator:</strong> Gradient icon addons</li>
                        <li><strong>Custom Error Display:</strong> Animated error messages with icons</li>
                        <li><strong>Custom Tooltip:</strong> Hover-triggered description tooltips</li>
                        <li><strong>Custom CSS Classes:</strong> Framework-agnostic styling</li>
                    </ul>
                </div>

                <CommandForm<SimpleCommand>
                    command={SimpleCommand}
                    fieldDecoratorComponent={CustomFieldDecorator}
                    errorDisplayComponent={CustomErrorDisplay}
                    tooltipComponent={CustomTooltip}
                    errorClassName="custom-error"
                    iconAddonClassName="custom-icon-addon"
                >
                    <InputTextField<SimpleCommand> 
                        value={c => c.name} 
                        title="Full Name"
                        placeholder="Enter your name (min 3 chars)"
                        icon={<span>👤</span>}
                        description="Your full legal name as it appears on official documents"
                    />
                    
                    <InputTextField<SimpleCommand> 
                        value={c => c.email} 
                        title="Email Address"
                        type="email" 
                        placeholder="you@example.com"
                        icon={<span>📧</span>}
                        description="We'll send a confirmation email to this address. We never share your email with third parties."
                    />

                    <div style={{ marginTop: '1.5rem' }}>
                        <button 
                            type="submit"
                            style={{
                                padding: '0.75rem 1.5rem',
                                background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                                color: 'white',
                                border: 'none',
                                borderRadius: '0.5rem',
                                fontWeight: 600,
                                cursor: 'pointer',
                                transition: 'transform 0.2s'
                            }}
                            onMouseEnter={(e) => e.currentTarget.style.transform = 'scale(1.05)'}
                            onMouseLeave={(e) => e.currentTarget.style.transform = 'scale(1)'}
                        >
                            Submit Form
                        </button>
                    </div>
                </CommandForm>

                <div style={{
                    marginTop: '2rem',
                    padding: '1rem',
                    backgroundColor: '#ecfdf5',
                    border: '1px solid #6ee7b7',
                    borderRadius: '0.5rem'
                }}>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#065f46' }}>
                        💡 <strong>Pro Tip:</strong> All customization components are optional. 
                        Mix and match them to create the perfect form experience for your application 
                        without depending on any specific UI framework.
                    </p>
                </div>
            </StoryContainer>
        );
    }
};
