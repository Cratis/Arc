// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useState } from 'react';
import { Meta, StoryObj } from '@storybook/react';
import { CommandForm } from './CommandForm';
import type { FieldContainerProps } from './CommandFormContext';
import { UserRegistrationCommand } from './UserRegistrationCommand';
import { 
    InputTextField, 
    NumberField, 
    TextAreaField, 
    CheckboxField, 
    RangeField,
    SelectField 
} from './fields';
import { Command, CommandValidator, CommandResult } from '@cratis/arc/commands';
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
                    showErrors={false}
                    initialValues={{
                        name: '',
                        email: '',
                    }}
                    onFieldChange={async (command, fieldName, _oldValue, _newValue, validationInfo) => {
                        // Check overall form validity
                        const result = await command.validate();
                        
                        setValidationState(prev => {
                            const newErrors = { ...prev.errors };
                            
                            // Use validationInfo for field-specific errors
                            if (validationInfo && !validationInfo.isValid && validationInfo.errors.length > 0) {
                                newErrors[fieldName] = validationInfo.errors[0];
                            } else {
                                delete newErrors[fieldName];
                            }
                            
                            return {
                                errors: newErrors,
                                // Can only submit if overall validation passes
                                canSubmit: result.isValid
                            };
                        });
                    }}
                >
                    <InputTextField<SimpleCommand> 
                        value={c => c.name} 
                        title="Name"
                        placeholder="Enter your name (min 3 chars)" 
                    />
                    {validationState.errors.name && (
                        <div style={{ 
                            color: 'var(--color-error)', 
                            fontSize: '0.875rem', 
                            marginTop: '0.25rem', 
                            marginBottom: '1rem',
                            display: 'flex',
                            alignItems: 'center',
                            gap: '0.375rem',
                            padding: '0.5rem 0.75rem',
                            backgroundColor: 'rgba(239, 68, 68, 0.08)',
                            border: '1px solid rgba(239, 68, 68, 0.3)',
                            borderRadius: 'var(--radius-md, 0.375rem)'
                        }}>
                            <span style={{ fontSize: '1rem', flexShrink: 0 }}>⚠️</span>
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
                        <div style={{ 
                            color: 'var(--color-error)', 
                            fontSize: '0.875rem', 
                            marginTop: '0.25rem', 
                            marginBottom: '1rem',
                            display: 'flex',
                            alignItems: 'center',
                            gap: '0.375rem',
                            padding: '0.5rem 0.75rem',
                            backgroundColor: 'rgba(239, 68, 68, 0.08)',
                            border: '1px solid rgba(239, 68, 68, 0.3)',
                            borderRadius: 'var(--radius-md, 0.375rem)'
                        }}>
                            <span style={{ fontSize: '1rem', flexShrink: 0 }}>⚠️</span>
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
        const CustomTitleContainer: React.FC<FieldContainerProps> = ({ title, errorMessage, children }) => {
            return (
                <div style={{ marginBottom: '1rem' }}>
                    {title && (
                        <div style={{ 
                            fontSize: '0.75rem', 
                            textTransform: 'uppercase', 
                            letterSpacing: '0.05em', 
                            marginBottom: '0.5rem',
                            color: 'var(--color-text-secondary)',
                            fontWeight: 600
                        }}>
                            {title}
                        </div>
                    )}
                    {children}
                    {errorMessage && (
                        <div style={{ 
                            color: 'var(--color-error)', 
                            fontSize: '0.875rem', 
                            marginTop: '0.25rem' 
                        }}>
                            {errorMessage}
                        </div>
                    )}
                </div>
            );
        };

        return (
            <StoryContainer size="sm" asCard>
                <h2>Custom Titles</h2>
                <p>
                    This form shows how to customize title rendering using a custom <code>fieldContainerComponent</code>.
                </p>
                <CommandForm<SimpleCommand>
                    command={SimpleCommand}
                    initialValues={{ name: '', email: '' }}
                    fieldContainerComponent={CustomTitleContainer}
                    showTitles={false}
                >
                    <InputTextField<SimpleCommand> 
                        value={c => c.name} 
                        title="Full Name *"
                        placeholder="Enter your full name" 
                    />
                    
                    <InputTextField<SimpleCommand> 
                        value={c => c.email} 
                        title="📧 Email Address"
                        type="email" 
                        placeholder="your.email@example.com" 
                    />

                    <button type="submit">Submit</button>
                </CommandForm>
                
                <div style={{
                    marginTop: '1.5rem',
                    padding: '1rem',
                    backgroundColor: '#eff6ff',
                    border: '1px solid #93c5fd',
                    borderRadius: '0.5rem'
                }}>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#1e40af' }}>
                        💡 <strong>Tip:</strong> Use a custom <code>fieldContainerComponent</code> to control 
                        how titles are rendered while keeping fields properly connected to the form.
                    </p>
                </div>
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
                    initialValues={{ name: '', email: '' }}
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
        const CustomContainer: React.FC<FieldContainerProps> = ({ title, errorMessage, children }) => {
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
                    initialValues={{ name: '', email: '' }}
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
                        style={{ flex: 1, display: 'flex', alignItems: 'stretch' }}
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
                            animation: 'fadeIn 0.1s ease-out'
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
                    initialValues={{ name: '', email: '' }}
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
export const MultiColumnLayout: Story = {
    render: () => {
        return (
            <StoryContainer size="lg" asCard>
                <h2>Multi-Column Layout</h2>
                <p>
                    Create responsive multi-column layouts using <code>CommandForm.Column</code>. 
                    Each column automatically adapts to different screen sizes.
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
                >
                    <h3>Personal Details</h3>
                    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: '2rem' }}>
                        <CommandForm.Column>
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
                            
                            <NumberField<UserRegistrationCommand> 
                                value={c => c.age} 
                                title="Age"
                                placeholder="Enter age" 
                                min={13} 
                                max={120} 
                            />
                        </CommandForm.Column>

                        <CommandForm.Column>
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
                            
                            <InputTextField<UserRegistrationCommand> 
                                value={c => c.birthDate} 
                                title="Birth Date"
                                type="date" 
                            />
                        </CommandForm.Column>
                    </div>

                    <h3 style={{ marginTop: '2rem' }}>Additional Information</h3>
                    <TextAreaField<UserRegistrationCommand> 
                        value={c => c.bio} 
                        title="Bio"
                        placeholder="Tell us about yourself" 
                        rows={4} 
                        required={false} 
                    />
                    
                    <button type="submit" style={{ marginTop: '1rem' }}>Register</button>
                </CommandForm>

                <div style={{
                    marginTop: '1.5rem',
                    padding: '1rem',
                    backgroundColor: '#eff6ff',
                    border: '1px solid #93c5fd',
                    borderRadius: '0.5rem'
                }}>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#1e40af' }}>
                        💡 <strong>Tip:</strong> Use CSS Grid or Flexbox to control column widths and responsive behavior.
                    </p>
                </div>
            </StoryContainer>
        );
    }
};

export const CustomValidationCallback: Story = {
    render: () => {
        const [customErrors, setCustomErrors] = useState<Record<string, string>>({});

        const handleFieldValidate = (command: SimpleCommand, fieldName: string, _oldValue: unknown, newValue: unknown): string | undefined => {
            // Custom validation logic that runs in addition to the command's validation
            if (fieldName === 'name') {
                const name = newValue as string;
                if (name && name.toLowerCase().includes('test')) {
                    return 'Name cannot contain the word "test"';
                }
                if (name && !/^[a-zA-Z\s]+$/.test(name)) {
                    return 'Name can only contain letters and spaces';
                }
            }
            
            if (fieldName === 'email') {
                const email = newValue as string;
                if (email && email.endsWith('@example.com')) {
                    return 'Please use a real email address, not example.com';
                }
            }
            
            return undefined;
        };

        return (
            <StoryContainer size="sm" asCard>
                <h2>Custom Validation Callback</h2>
                <p>
                    Use <code>onFieldValidate</code> to add custom validation logic beyond the command's built-in validation.
                    This is perfect for business rules that need access to runtime data.
                </p>
                
                <CommandForm<SimpleCommand>
                    command={SimpleCommand}
                    onFieldValidate={(command, fieldName, oldValue, newValue) => {
                        const error = handleFieldValidate(command, fieldName, oldValue, newValue);
                        if (error) {
                            setCustomErrors(prev => ({ ...prev, [fieldName]: error }));
                        } else {
                            setCustomErrors(prev => {
                                const { [fieldName]: removed, ...rest } = prev;
                                return rest;
                            });
                        }
                        return error;
                    }}
                >
                    <InputTextField<SimpleCommand> 
                        value={c => c.name} 
                        title="Name"
                        placeholder="Try typing 'test' or numbers" 
                    />
                    {customErrors.name && (
                        <div style={{ 
                            color: 'var(--color-error)', 
                            fontSize: '0.875rem', 
                            marginTop: '0.25rem',
                            marginBottom: '1rem',
                            padding: '0.5rem',
                            backgroundColor: 'rgba(239, 68, 68, 0.1)',
                            borderRadius: '0.25rem'
                        }}>
                            {customErrors.name}
                        </div>
                    )}
                    
                    <InputTextField<SimpleCommand> 
                        value={c => c.email} 
                        title="Email"
                        type="email" 
                        placeholder="Try @example.com" 
                    />
                    {customErrors.email && (
                        <div style={{ 
                            color: 'var(--color-error)', 
                            fontSize: '0.875rem', 
                            marginTop: '0.25rem',
                            marginBottom: '1rem',
                            padding: '0.5rem',
                            backgroundColor: 'rgba(239, 68, 68, 0.1)',
                            borderRadius: '0.25rem'
                        }}>
                            {customErrors.email}
                        </div>
                    )}

                    <button type="submit" style={{ marginTop: '1rem' }}>Submit</button>
                </CommandForm>

                <div style={{
                    marginTop: '1.5rem',
                    padding: '1rem',
                    backgroundColor: '#fef3c7',
                    border: '1px solid #fbbf24',
                    borderRadius: '0.5rem'
                }}>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#92400e' }}>
                        ⚠️ <strong>Try These:</strong> Type "test123" in the name field or use @example.com email to see custom validation in action.
                    </p>
                </div>
            </StoryContainer>
        );
    }
};

export const BeforeExecuteCallback: Story = {
    render: () => {
        const [preprocessedData, setPreprocessedData] = useState<string>('');

        return (
            <StoryContainer size="sm" asCard>
                <h2>Before Execute Callback</h2>
                <p>
                    Use <code>onBeforeExecute</code> to transform data before submission. 
                    Perfect for sanitizing input, formatting data, or adding computed fields.
                </p>
                
                <CommandForm<SimpleCommand>
                    command={SimpleCommand}
                    initialValues={{ name: '', email: '' }}
                    onBeforeExecute={(command) => {
                        // Transform the data before execution
                        command.name = command.name.trim().replace(/\s+/g, ' '); // Normalize whitespace
                        command.email = command.email.toLowerCase().trim(); // Lowercase email
                        
                        setPreprocessedData(JSON.stringify({ name: command.name, email: command.email }, null, 2));
                        
                        return command;
                    }}
                >
                    <InputTextField<SimpleCommand> 
                        value={c => c.name} 
                        title="Name"
                        placeholder="Try   extra   spaces" 
                    />
                    
                    <InputTextField<SimpleCommand> 
                        value={c => c.email} 
                        title="Email"
                        type="email" 
                        placeholder="Try UPPERCASE@EMAIL.COM" 
                    />

                    <button type="submit" style={{ marginTop: '1rem' }}>
                        Submit (Data will be preprocessed)
                    </button>
                </CommandForm>

                {preprocessedData && (
                    <div style={{ marginTop: '1.5rem' }}>
                        <h4 style={{ marginTop: 0, color: 'var(--color-success)' }}>✓ Preprocessed Data:</h4>
                        <pre style={{
                            backgroundColor: 'var(--color-background-secondary)',
                            padding: '1rem',
                            borderRadius: '0.5rem',
                            overflow: 'auto',
                            fontSize: '0.875rem'
                        }}>
                            {preprocessedData}
                        </pre>
                    </div>
                )}

                <div style={{
                    marginTop: '1.5rem',
                    padding: '1rem',
                    backgroundColor: '#eff6ff',
                    border: '1px solid #93c5fd',
                    borderRadius: '0.5rem'
                }}>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#1e40af' }}>
                        💡 <strong>Use Cases:</strong> Trimming whitespace, normalizing data formats, adding timestamps, or computing derived fields.
                    </p>
                </div>
            </StoryContainer>
        );
    }
};

export const CurrentValuesVsInitialValues: Story = {
    render: () => {
        const [showInitial, setShowInitial] = useState(false);
        const [currentData, setCurrentData] = useState({ name: 'Jane Doe', email: 'jane@example.com' });

        return (
            <StoryContainer size="sm" asCard>
                <h2>Current Values vs Initial Values</h2>
                <p>
                    <code>initialValues</code> sets the starting state, while <code>currentValues</code> 
                    updates the form when external data changes. Use currentValues for editing existing records.
                </p>

                <div style={{ 
                    marginBottom: '1.5rem', 
                    display: 'flex', 
                    gap: '1rem',
                    padding: '1rem',
                    backgroundColor: 'var(--color-background-secondary)',
                    borderRadius: '0.5rem'
                }}>
                    <button 
                        onClick={() => setCurrentData({ name: 'John Smith', email: 'john@example.com' })}
                        style={{ fontSize: '0.875rem' }}
                    >
                        Load User 1
                    </button>
                    <button 
                        onClick={() => setCurrentData({ name: 'Alice Johnson', email: 'alice@example.com' })}
                        style={{ fontSize: '0.875rem' }}
                    >
                        Load User 2
                    </button>
                    <button 
                        onClick={() => setCurrentData({ name: '', email: '' })}
                        style={{ fontSize: '0.875rem' }}
                    >
                        Clear
                    </button>
                </div>

                <div style={{ marginBottom: '1rem' }}>
                    <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', cursor: 'pointer' }}>
                        <input 
                            type="checkbox" 
                            checked={showInitial}
                            onChange={(e) => setShowInitial(e.target.checked)}
                        />
                        <span>Use initialValues (form state independent of currentData)</span>
                    </label>
                </div>
                
                <CommandForm<SimpleCommand>
                    command={SimpleCommand}
                    initialValues={showInitial ? { name: 'Initial Name', email: 'initial@example.com' } : undefined}
                    currentValues={!showInitial ? currentData : undefined}
                >
                    <InputTextField<SimpleCommand> 
                        value={c => c.name} 
                        title="Name"
                        placeholder="Enter name" 
                    />
                    
                    <InputTextField<SimpleCommand> 
                        value={c => c.email} 
                        title="Email"
                        type="email" 
                        placeholder="Enter email" 
                    />

                    <button type="submit" style={{ marginTop: '1rem' }}>Save Changes</button>
                </CommandForm>

                <div style={{
                    marginTop: '1.5rem',
                    padding: '1rem',
                    backgroundColor: '#f0fdf4',
                    border: '1px solid #86efac',
                    borderRadius: '0.5rem'
                }}>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#166534' }}>
                        <strong>Current Mode:</strong> {showInitial ? 'initialValues (static)' : 'currentValues (reactive)'}
                    </p>
                    <p style={{ margin: '0.5rem 0 0 0', fontSize: '0.875rem', color: '#166534' }}>
                        {showInitial 
                            ? 'Form ignores external data changes. Good for new records.' 
                            : 'Form updates when currentData changes. Perfect for editing existing records.'}
                    </p>
                </div>
            </StoryContainer>
        );
    }
};

export const FieldWithIcons: Story = {
    render: () => {
        return (
            <StoryContainer size="sm" asCard>
                <h2>Fields with Icons</h2>
                <p>
                    Add visual context to form fields using the <code>icon</code> prop. 
                    Icons help users quickly identify field purposes.
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
                >
                    <InputTextField<UserRegistrationCommand> 
                        value={c => c.username} 
                        title="Username"
                        placeholder="Enter username"
                        icon={<span style={{ fontSize: '1.25rem' }}>👤</span>}
                    />
                    
                    <InputTextField<UserRegistrationCommand> 
                        value={c => c.email} 
                        title="Email Address"
                        type="email" 
                        placeholder="you@example.com"
                        icon={<span style={{ fontSize: '1.25rem' }}>📧</span>}
                    />
                    
                    <InputTextField<UserRegistrationCommand> 
                        value={c => c.password} 
                        title="Password"
                        type="password" 
                        placeholder="Enter password"
                        icon={<span style={{ fontSize: '1.25rem' }}>🔒</span>}
                    />
                    
                    <InputTextField<UserRegistrationCommand> 
                        value={c => c.birthDate} 
                        title="Birth Date"
                        type="date"
                        icon={<span style={{ fontSize: '1.25rem' }}>📅</span>}
                    />
                    
                    <SelectField<UserRegistrationCommand>
                        value={c => c.role}
                        title="Role"
                        options={roleOptions}
                        optionIdField="id"
                        optionLabelField="name"
                        placeholder="Select a role"
                        icon={<span style={{ fontSize: '1.25rem' }}>🎭</span>}
                    />

                    <button type="submit" style={{ marginTop: '1rem' }}>Register</button>
                </CommandForm>

                <div style={{
                    marginTop: '1.5rem',
                    padding: '1rem',
                    backgroundColor: '#fef3c7',
                    border: '1px solid #fbbf24',
                    borderRadius: '0.5rem'
                }}>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#92400e' }}>
                        💡 <strong>Tip:</strong> Icons can be emoji, SVG, or any React element. Combine with custom fieldDecoratorComponent for advanced styling.
                    </p>
                </div>
            </StoryContainer>
        );
    }
};

export const FieldWithTooltips: Story = {
    render: () => {
        return (
            <StoryContainer size="sm" asCard>
                <h2>Fields with Tooltips</h2>
                <p>
                    Use the <code>description</code> prop to add helpful tooltips to fields. 
                    Combine with a custom <code>tooltipComponent</code> for styled tooltips.
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
                >
                    <InputTextField<UserRegistrationCommand> 
                        value={c => c.username} 
                        title="Username"
                        placeholder="Enter username"
                        description="Choose a unique username between 3-20 characters. Only letters, numbers, and underscores allowed."
                    />
                    
                    <InputTextField<UserRegistrationCommand> 
                        value={c => c.email} 
                        title="Email Address"
                        type="email" 
                        placeholder="you@example.com"
                        description="We'll send account verification and important updates to this address. Your email is never shared with third parties."
                    />
                    
                    <InputTextField<UserRegistrationCommand> 
                        value={c => c.password} 
                        title="Password"
                        type="password" 
                        placeholder="Enter password"
                        description="Use at least 8 characters with a mix of uppercase, lowercase, numbers, and special characters for a strong password."
                    />
                    
                    <NumberField<UserRegistrationCommand> 
                        value={c => c.age} 
                        title="Age"
                        placeholder="Enter age"
                        min={13}
                        max={120}
                        description="You must be at least 13 years old to create an account."
                    />

                    <button type="submit" style={{ marginTop: '1rem' }}>Register</button>
                </CommandForm>

                <div style={{
                    marginTop: '1.5rem',
                    padding: '1rem',
                    backgroundColor: '#eff6ff',
                    border: '1px solid #93c5fd',
                    borderRadius: '0.5rem'
                }}>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#1e40af' }}>
                        💡 <strong>Default Behavior:</strong> Descriptions are shown as the field's <code>title</code> attribute. 
                        Provide a custom tooltipComponent to render styled tooltips.
                    </p>
                </div>
            </StoryContainer>
        );
    }
};

export const CustomCSSClasses: Story = {
    render: () => {
        return (
            <StoryContainer size="sm" asCard>
                <style>{`
                    .my-custom-error {
                        background: linear-gradient(135deg, #fee2e2 0%, #fecaca 100%);
                        border-left: 4px solid #dc2626;
                        padding: 0.75rem;
                        margin-top: 0.5rem;
                        border-radius: 0.25rem;
                        font-weight: 500;
                        color: #991b1b;
                    }
                    
                    .my-custom-icon-addon {
                        background: linear-gradient(135deg, #dbeafe 0%, #bfdbfe 100%);
                        border: 2px solid #3b82f6;
                        padding: 0.5rem 0.75rem;
                        border-radius: 0.375rem 0 0 0.375rem;
                        font-size: 1.25rem;
                    }
                `}</style>

                <h2>Custom CSS Classes</h2>
                <p>
                    Use <code>errorClassName</code> and <code>iconAddonClassName</code> to apply 
                    custom CSS classes for framework-agnostic styling.
                </p>
                
                <CommandForm<SimpleCommand>
                    command={SimpleCommand}
                    errorClassName="my-custom-error"
                    iconAddonClassName="my-custom-icon-addon"
                    initialValues={{ name: '', email: '' }}
                >
                    <InputTextField<SimpleCommand> 
                        value={c => c.name} 
                        title="Name"
                        placeholder="Enter your name (min 3 chars)"
                        icon={<span>👤</span>}
                    />
                    
                    <InputTextField<SimpleCommand> 
                        value={c => c.email} 
                        title="Email"
                        type="email" 
                        placeholder="you@example.com"
                        icon={<span>📧</span>}
                    />

                    <button type="submit" style={{ marginTop: '1rem' }}>Submit</button>
                </CommandForm>

                <div style={{
                    marginTop: '1.5rem',
                    padding: '1rem',
                    backgroundColor: '#f0fdf4',
                    border: '1px solid #86efac',
                    borderRadius: '0.5rem'
                }}>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#166534' }}>
                        💡 <strong>Framework Agnostic:</strong> Use your preferred CSS methodology - 
                        vanilla CSS, CSS Modules, Tailwind, styled-components, or any other approach.
                    </p>
                </div>
            </StoryContainer>
        );
    }
};

export const MixedChildrenWithFields: Story = {
    render: () => {
        const [acceptMarketing, setAcceptMarketing] = useState(false);

        return (
            <StoryContainer size="sm" asCard>
                <h2>Mixed Children with Form Fields</h2>
                <p>
                    Combine form fields with any other React elements - headings, paragraphs, 
                    cards, custom components, etc. Create rich, structured forms.
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
                >
                    <div style={{
                        backgroundColor: '#eff6ff',
                        padding: '1rem',
                        borderRadius: '0.5rem',
                        marginBottom: '1.5rem',
                        border: '1px solid #bfdbfe'
                    }}>
                        <h3 style={{ margin: 0, color: '#1e40af', fontSize: '1rem' }}>Account Information</h3>
                        <p style={{ margin: '0.5rem 0 0 0', fontSize: '0.875rem', color: '#1e40af' }}>
                            Let's create your account. All fields are required.
                        </p>
                    </div>

                    <InputTextField<UserRegistrationCommand> 
                        value={c => c.username} 
                        title="Username"
                        placeholder="Enter username"
                    />
                    
                    <InputTextField<UserRegistrationCommand> 
                        value={c => c.email} 
                        title="Email Address"
                        type="email" 
                        placeholder="you@example.com"
                    />

                    <div style={{
                        backgroundColor: '#fef3c7',
                        padding: '1rem',
                        borderRadius: '0.5rem',
                        margin: '1.5rem 0',
                        border: '1px solid #fbbf24'
                    }}>
                        <p style={{ margin: 0, fontSize: '0.875rem', color: '#92400e' }}>
                            💡 <strong>Password Security:</strong> Use at least 8 characters with a mix of letters, 
                            numbers and symbols.
                        </p>
                    </div>
                    
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

                    <hr style={{ margin: '2rem 0', border: 'none', borderTop: '1px solid var(--color-border)' }} />

                    <h3 style={{ marginTop: 0 }}>Marketing Preferences</h3>
                    
                    <label style={{ 
                        display: 'flex', 
                        alignItems: 'center', 
                        gap: '0.5rem',
                        padding: '1rem',
                        backgroundColor: 'var(--color-background-secondary)',
                        borderRadius: '0.5rem',
                        cursor: 'pointer',
                        marginBottom: '1rem'
                    }}>
                        <input 
                            type="checkbox" 
                            checked={acceptMarketing}
                            onChange={(e) => setAcceptMarketing(e.target.checked)}
                        />
                        <span style={{ fontSize: '0.875rem' }}>
                            I want to receive marketing emails and special offers
                        </span>
                    </label>

                    <CheckboxField<UserRegistrationCommand> 
                        value={c => c.agreeToTerms} 
                        label="I agree to the terms and conditions *"
                    />

                    <button type="submit" style={{ marginTop: '1.5rem' }}>Create Account</button>
                </CommandForm>

                <div style={{
                    marginTop: '1.5rem',
                    padding: '1rem',
                    backgroundColor: '#f0fdf4',
                    border: '1px solid #86efac',
                    borderRadius: '0.5rem'
                }}>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#166534' }}>
                        ✓ <strong>Flexible Structure:</strong> Form fields work seamlessly alongside any other React content.
                    </p>
                </div>
            </StoryContainer>
        );
    }
};

export const ValidationOnBlur: Story = {
    render: () => {
        return (
            <StoryContainer size="sm" asCard>
                <h2>Validation on Blur (Default)</h2>
                <p>
                    Fields are validated only when you leave them (blur). This is the default behavior 
                    and provides the best user experience - errors appear after the user finishes editing a field.
                </p>
                
                <CommandForm<SimpleCommand>
                    command={SimpleCommand}
                    validateOn="blur"
                >
                    <InputTextField<SimpleCommand> 
                        value={c => c.name} 
                        title="Name"
                        placeholder="Enter name (min 3 chars)" 
                    />
                    
                    <InputTextField<SimpleCommand> 
                        value={c => c.email} 
                        title="Email"
                        type="email" 
                        placeholder="Enter valid email" 
                    />

                    <button type="submit" style={{ marginTop: '1rem' }}>Submit</button>
                </CommandForm>

                <div style={{
                    marginTop: '1.5rem',
                    padding: '1rem',
                    backgroundColor: '#eff6ff',
                    border: '1px solid #93c5fd',
                    borderRadius: '0.5rem'
                }}>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#1e40af' }}>
                        💡 <strong>Try it:</strong> Start typing in a field, then click or tab away. Errors appear only after you leave the field.
                    </p>
                </div>
            </StoryContainer>
        );
    }
};

export const ValidationOnChange: Story = {
    render: () => {
        return (
            <StoryContainer size="sm" asCard>
                <h2>Validation on Change</h2>
                <p>
                    Fields are validated immediately as you type. Use this when you need instant feedback,
                    but be aware it can feel aggressive to users.
                </p>
                
                <CommandForm<SimpleCommand>
                    command={SimpleCommand}
                    validateOn="change"
                >
                    <InputTextField<SimpleCommand> 
                        value={c => c.name} 
                        title="Name"
                        placeholder="Enter name (min 3 chars)" 
                    />
                    
                    <InputTextField<SimpleCommand> 
                        value={c => c.email} 
                        title="Email"
                        type="email" 
                        placeholder="Enter valid email" 
                    />

                    <button type="submit" style={{ marginTop: '1rem' }}>Submit</button>
                </CommandForm>

                <div style={{
                    marginTop: '1.5rem',
                    padding: '1rem',
                    backgroundColor: '#fef3c7',
                    border: '1px solid #fcd34d',
                    borderRadius: '0.5rem'
                }}>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#92400e' }}>
                        ⚠️ <strong>Notice:</strong> Errors appear immediately as you type. This can be helpful but may feel intrusive.
                    </p>
                </div>
            </StoryContainer>
        );
    }
};

export const ValidationOnBoth: Story = {
    render: () => {
        return (
            <StoryContainer size="sm" asCard>
                <h2>Validation on Both Change and Blur</h2>
                <p>
                    Fields are validated both when you type and when you leave them. This provides
                    continuous feedback once you start editing a field.
                </p>
                
                <CommandForm<SimpleCommand>
                    command={SimpleCommand}
                    validateOn="both"
                >
                    <InputTextField<SimpleCommand> 
                        value={c => c.name} 
                        title="Name"
                        placeholder="Enter name (min 3 chars)" 
                    />
                    
                    <InputTextField<SimpleCommand> 
                        value={c => c.email} 
                        title="Email"
                        type="email" 
                        placeholder="Enter valid email" 
                    />

                    <button type="submit" style={{ marginTop: '1rem' }}>Submit</button>
                </CommandForm>

                <div style={{
                    marginTop: '1.5rem',
                    padding: '1rem',
                    backgroundColor: '#eff6ff',
                    border: '1px solid #93c5fd',
                    borderRadius: '0.5rem'
                }}>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#1e40af' }}>
                        💡 <strong>Best of both:</strong> Validation happens as you type AND when you leave fields.
                    </p>
                </div>
            </StoryContainer>
        );
    }
};

export const ValidationOnInit: Story = {
    render: () => {
        return (
            <StoryContainer size="sm" asCard>
                <h2>Validation on Initialization</h2>
                <p>
                    The form is validated immediately when it loads. This is useful when you want to 
                    show validation errors right away, such as when editing a record that has invalid data.
                </p>
                
                <CommandForm<SimpleCommand>
                    command={SimpleCommand}
                    validateOnInit={true}
                    initialValues={{ name: 'ab', email: 'invalid' }}
                >
                    <InputTextField<SimpleCommand> 
                        value={c => c.name} 
                        title="Name"
                        placeholder="Enter name (min 3 chars)" 
                    />
                    
                    <InputTextField<SimpleCommand> 
                        value={c => c.email} 
                        title="Email"
                        type="email" 
                        placeholder="Enter valid email" 
                    />

                    <button type="submit" style={{ marginTop: '1rem' }}>Submit</button>
                </CommandForm>

                <div style={{
                    marginTop: '1.5rem',
                    padding: '1rem',
                    backgroundColor: '#fef3c7',
                    border: '1px solid #fcd34d',
                    borderRadius: '0.5rem'
                }}>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#92400e' }}>
                        ⚠️ <strong>Notice:</strong> Errors are shown immediately on load because the initial values are invalid.
                    </p>
                </div>
            </StoryContainer>
        );
    }
};

export const ValidationAllFieldsOnChange: Story = {
    render: () => {
        return (
            <StoryContainer size="sm" asCard>
                <h2>Validate All Fields on Change</h2>
                <p>
                    When enabled, changing any field validates the entire form. This is useful for 
                    cross-field validation rules where one field affects another.
                </p>
                
                <CommandForm<SimpleCommand>
                    command={SimpleCommand}
                    validateOn="blur"
                    validateAllFieldsOnChange={true}
                >
                    <InputTextField<SimpleCommand> 
                        value={c => c.name} 
                        title="Name"
                        placeholder="Enter name (min 3 chars)" 
                    />
                    
                    <InputTextField<SimpleCommand> 
                        value={c => c.email} 
                        title="Email"
                        type="email" 
                        placeholder="Enter valid email" 
                    />

                    <button type="submit" style={{ marginTop: '1rem' }}>Submit</button>
                </CommandForm>

                <div style={{
                    marginTop: '1.5rem',
                    padding: '1rem',
                    backgroundColor: '#eff6ff',
                    border: '1px solid #93c5fd',
                    borderRadius: '0.5rem'
                }}>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#1e40af' }}>
                        💡 <strong>Try it:</strong> Enter invalid data in both fields. When you blur one field, both will be validated and show errors.
                    </p>
                </div>
            </StoryContainer>
        );
    }
};

// Server-validated command that checks for reserved names
class ServerValidatedCommand extends Command {
    readonly route: string = '/api/server-validated';
    readonly validation: ServerValidatedCommandValidator = new ServerValidatedCommandValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('username', String),
        new PropertyDescriptor('email', String),
    ];

    username = '';
    email = '';

    constructor() {
        super(Object, false);
    }

    get requestParameters(): string[] {
        return [];
    }

    get properties(): string[] {
        return ['username', 'email'];
    }

    // Override validate to simulate server-side validation
    async validate() {
        // Simulate network delay
        await new Promise(resolve => setTimeout(resolve, 500));

        const result = await super.validate();

        // Simulate server-side validation: check for reserved usernames
        const reservedUsernames = ['admin', 'root', 'system'];
        if (reservedUsernames.includes(this.username.toLowerCase())) {
            const validationResult = CommandResult.validationFailed([
                {
                    severity: 0,
                    message: 'This username is reserved and cannot be used.',
                    members: ['username'],
                    state: {}
                }
            ]);
            return validationResult;
        }

        // Simulate checking if email is already registered
        const registeredEmails = ['test@example.com', 'admin@example.com'];
        if (registeredEmails.includes(this.email.toLowerCase())) {
            return CommandResult.validationFailed([
                {
                    severity: 0,
                    message: 'This email address is already registered.',
                    members: ['email'],
                    state: {}
                }
            ]);
        }

        // All good!
        return result;
    }
}

class ServerValidatedCommandValidator extends CommandValidator<ServerValidatedCommand> {
    constructor() {
        super();
        this.ruleFor(c => c.username).notEmpty().minLength(3).maxLength(20);
        this.ruleFor(c => c.email).notEmpty().emailAddress();
    }
}

export const AutoServerValidation: Story = {
    render: () => {
        const [validationLog, setValidationLog] = useState<string[]>([]);
        const [serverCallCount, setServerCallCount] = useState(0);

        // Create a command class that logs server validation calls
        class TrackedServerValidatedCommand extends ServerValidatedCommand {
            async validate() {
                const callNumber = serverCallCount + 1;
                setServerCallCount(callNumber);
                setValidationLog(prev => [
                    ...prev,
                    `[${new Date().toLocaleTimeString()}] Server validation #${callNumber}: username="${this.username}", email="${this.email}"`
                ]);
                return await super.validate();
            }
        }

        return (
            <StoryContainer size="md" asCard>
                <h2>Auto Server Validation</h2>
                <p>
                    When <code>autoServerValidate</code> is enabled, the form automatically calls the 
                    server validation method when all client-side validations pass. This provides 
                    real-time feedback for server-side validation rules (like checking if a username 
                    is already taken).
                </p>
                
                <CommandForm<ServerValidatedCommand>
                    command={TrackedServerValidatedCommand}
                    validateOn="change"
                    autoServerValidate={true}
                >
                    <InputTextField<ServerValidatedCommand> 
                        value={c => c.username} 
                        title="Username"
                        placeholder="Enter username (min 3 chars)" 
                    />
                    
                    <InputTextField<ServerValidatedCommand> 
                        value={c => c.email} 
                        title="Email"
                        type="email" 
                        placeholder="Enter email address" 
                    />

                    <button type="submit" style={{ marginTop: '1rem' }}>Submit</button>
                </CommandForm>

                <div style={{
                    marginTop: '1.5rem',
                    padding: '1rem',
                    backgroundColor: '#fef3c7',
                    border: '1px solid #fcd34d',
                    borderRadius: '0.5rem'
                }}>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#92400e', marginBottom: '0.5rem' }}>
                        <strong>Reserved Usernames:</strong> admin, root, system<br />
                        <strong>Registered Emails:</strong> test@example.com, admin@example.com
                    </p>
                </div>

                <div style={{
                    marginTop: '1.5rem',
                    padding: '1rem',
                    backgroundColor: '#eff6ff',
                    border: '1px solid #93c5fd',
                    borderRadius: '0.5rem'
                }}>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#1e40af' }}>
                        💡 <strong>Try it:</strong> Type valid values in both fields. Once all client validations pass, 
                        the server validation will automatically run (notice the brief delay simulating a network call). 
                        Try entering reserved usernames or registered emails to see server-side validation errors.
                    </p>
                </div>

                {validationLog.length > 0 && (
                    <div style={{
                        marginTop: '1.5rem',
                        padding: '1rem',
                        backgroundColor: '#f3f4f6',
                        border: '1px solid #d1d5db',
                        borderRadius: '0.5rem'
                    }}>
                        <h3 style={{ marginTop: 0, fontSize: '1rem' }}>
                            Server Validation Log ({serverCallCount} call{serverCallCount !== 1 ? 's' : ''})
                        </h3>
                        <div style={{
                            maxHeight: '200px',
                            overflowY: 'auto',
                            fontFamily: 'monospace',
                            fontSize: '0.75rem',
                            backgroundColor: '#ffffff',
                            padding: '0.5rem',
                            borderRadius: '0.25rem'
                        }}>
                            {validationLog.map((log, index) => (
                                <div key={index} style={{ marginBottom: '0.25rem' }}>
                                    {log}
                                </div>
                            ))}
                        </div>
                    </div>
                )}
            </StoryContainer>
        );
    }
};
