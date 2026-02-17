// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using NSubstitute;

namespace Cratis.Arc.Validation.for_DiscoverableModelValidator.given;

public class a_discoverable_model_validator : Specification
{
    protected DiscoverableModelValidator _modelValidator;
    protected IValidator _validator;
    protected ModelValidationContext _validationContext;
    protected ActionContext _actionContext;
    protected ActionDescriptor _actionDescriptor;
    protected ModelMetadata _modelMetadata;
    protected object _model;

    void Establish()
    {
        _validator = Substitute.For<IValidator>();
        _modelValidator = new DiscoverableModelValidator(_validator);

        var httpContext = new DefaultHttpContext();
        _actionDescriptor = new ActionDescriptor
        {
            FilterDescriptors = []
        };
        _actionContext = new ActionContext(httpContext, new RouteData(), _actionDescriptor);

        var modelMetadataProvider = new EmptyModelMetadataProvider();
        _modelMetadata = modelMetadataProvider.GetMetadataForType(typeof(TestModel));

        _model = new TestModel();
        _validationContext = new ModelValidationContext(_actionContext, _modelMetadata, modelMetadataProvider, null, _model);
    }

    protected class TestModel
    {
        public string Value { get; set; } = string.Empty;
    }
}
