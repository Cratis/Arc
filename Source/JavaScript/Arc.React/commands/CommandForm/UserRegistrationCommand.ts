// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command, CommandValidator } from '@cratis/arc/commands';
import { useCommand, SetCommandValues, ClearCommandValues } from '../';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import '@cratis/arc/validation';

export interface IUserRegistrationCommand {
    username?: string;
    email?: string;
    password?: string;
    confirmPassword?: string;
    age?: number;
    bio?: string;
    favoriteColor?: string;
    birthDate?: string;
    agreeToTerms?: boolean;
    experienceLevel?: number;
    role?: string;
}

export class UserRegistrationCommandValidator extends CommandValidator<IUserRegistrationCommand> {
    constructor() {
        super();
        this.ruleFor(c => c.username).notEmpty().minLength(3).maxLength(20);
        this.ruleFor(c => c.email).notEmpty().emailAddress();
        this.ruleFor(c => c.password).notEmpty().minLength(8);
        this.ruleFor(c => c.age).greaterThanOrEqual(13).lessThanOrEqual(120);
    }
}

export class UserRegistrationCommand extends Command<IUserRegistrationCommand, object> implements IUserRegistrationCommand {
    readonly route: string = '/api/users/register';
    readonly validation: CommandValidator = new UserRegistrationCommandValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('username', String),
        new PropertyDescriptor('email', String),
        new PropertyDescriptor('password', String),
        new PropertyDescriptor('confirmPassword', String),
        new PropertyDescriptor('age', Number),
        new PropertyDescriptor('bio', String),
        new PropertyDescriptor('favoriteColor', String),
        new PropertyDescriptor('birthDate', String),
        new PropertyDescriptor('agreeToTerms', Boolean),
        new PropertyDescriptor('experienceLevel', Number),
        new PropertyDescriptor('role', String),
    ];

    private _username!: string;
    private _email!: string;
    private _password!: string;
    private _confirmPassword!: string;
    private _age!: number;
    private _bio!: string;
    private _favoriteColor!: string;
    private _birthDate!: string;
    private _agreeToTerms!: boolean;
    private _experienceLevel!: number;
    private _role!: string;

    constructor() {
        super(Object, false);
    }

    get requestParameters(): string[] {
        return [];
    }

    get properties(): string[] {
        return [
            'username',
            'email',
            'password',
            'confirmPassword',
            'age',
            'bio',
            'favoriteColor',
            'birthDate',
            'agreeToTerms',
            'experienceLevel',
            'role',
        ];
    }

    get username(): string {
        return this._username;
    }

    set username(value: string) {
        this._username = value;
        this.propertyChanged('username');
    }

    get email(): string {
        return this._email;
    }

    set email(value: string) {
        this._email = value;
        this.propertyChanged('email');
    }

    get password(): string {
        return this._password;
    }

    set password(value: string) {
        this._password = value;
        this.propertyChanged('password');
    }

    get confirmPassword(): string {
        return this._confirmPassword;
    }

    set confirmPassword(value: string) {
        this._confirmPassword = value;
        this.propertyChanged('confirmPassword');
    }

    get age(): number {
        return this._age;
    }

    set age(value: number) {
        this._age = value;
        this.propertyChanged('age');
    }

    get bio(): string {
        return this._bio;
    }

    set bio(value: string) {
        this._bio = value;
        this.propertyChanged('bio');
    }

    get favoriteColor(): string {
        return this._favoriteColor;
    }

    set favoriteColor(value: string) {
        this._favoriteColor = value;
        this.propertyChanged('favoriteColor');
    }

    get birthDate(): string {
        return this._birthDate;
    }

    set birthDate(value: string) {
        this._birthDate = value;
        this.propertyChanged('birthDate');
    }

    get agreeToTerms(): boolean {
        return this._agreeToTerms;
    }

    set agreeToTerms(value: boolean) {
        this._agreeToTerms = value;
        this.propertyChanged('agreeToTerms');
    }

    get experienceLevel(): number {
        return this._experienceLevel;
    }

    set experienceLevel(value: number) {
        this._experienceLevel = value;
        this.propertyChanged('experienceLevel');
    }

    get role(): string {
        return this._role;
    }

    set role(value: string) {
        this._role = value;
        this.propertyChanged('role');
    }

    static use(initialValues?: IUserRegistrationCommand): [UserRegistrationCommand, SetCommandValues<IUserRegistrationCommand>, ClearCommandValues, number] {
        return useCommand<UserRegistrationCommand, IUserRegistrationCommand>(UserRegistrationCommand, initialValues);
    }
}
