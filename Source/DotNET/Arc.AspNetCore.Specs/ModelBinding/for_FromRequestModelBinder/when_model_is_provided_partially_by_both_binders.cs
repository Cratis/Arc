// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Cratis.Arc.ModelBinding.for_FromRequestModelBinder;

public class when_model_is_provided_partially_by_both_binders : Specification
{
    FromRequestModelBinder _binder;
    IModelBinder _bodyBinder;
    IModelBinder _complexBinder;

    ModelBindingContext _context;
    ModelBindingResult _result;

    ModelBindingResult _finalResult;

    TheModel _bodyModel;
    TheModel _complexModel;

    void Establish()
    {
        _bodyModel = new TheModel(42, "forty two", 0, null!);
        _complexModel = new TheModel(0, null!, 43, "forty three");

        _context = Substitute.For<ModelBindingContext>();
        _context.Result.Returns(_ => _result);
        _context.When(_ => _.Result = Arg.Any<ModelBindingResult>()).Do(_ => _finalResult = _.Arg<ModelBindingResult>());

        _bodyBinder = Substitute.For<IModelBinder>();
        _bodyBinder.BindModelAsync(_context).Returns((_) =>
        {
            _result = ModelBindingResult.Success(_bodyModel);
            return Task.CompletedTask;
        });

        _complexBinder = Substitute.For<IModelBinder>();
        _complexBinder.BindModelAsync(_context).Returns((_) =>
        {
            _result = ModelBindingResult.Success(_complexModel);
            return Task.CompletedTask;
        });

        _binder = new(_bodyBinder, _complexBinder);
    }

    Task Because() => _binder.BindModelAsync(_context);

    [Fact] void should_hold_body_model_int() => ((TheModel)_finalResult.Model).intValue.ShouldEqual(_bodyModel.intValue);
    [Fact] void should_hold_body_model_string() => ((TheModel)_finalResult.Model).stringValue.ShouldEqual(_bodyModel.stringValue);
    [Fact] void should_hold_complex_model_int() => ((TheModel)_finalResult.Model).secondIntValue.ShouldEqual(_complexModel.secondIntValue);
    [Fact] void should_hold_complex_model_string() => ((TheModel)_finalResult.Model).secondStringValue.ShouldEqual(_complexModel.secondStringValue);
}
