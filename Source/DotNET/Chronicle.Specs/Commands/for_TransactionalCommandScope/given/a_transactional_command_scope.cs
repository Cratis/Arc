// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.given;

public class a_transactional_command_scope : Specification
{
    protected TransactionalCommandScope _scope;
    protected IUnitOfWorkManager _unitOfWorkManager;
    protected IUnitOfWork _unitOfWork;
    protected IServiceProvider _serviceProvider;
    protected CommandContext _context;
    protected CorrelationId _correlationId;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWorkManager = Substitute.For<IUnitOfWorkManager>();
        _unitOfWorkManager.HasCurrent.Returns(false);
        _unitOfWorkManager.Begin(Arg.Any<CorrelationId>()).Returns(_unitOfWork);
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceProvider.GetService(typeof(IUnitOfWorkManager)).Returns(_unitOfWorkManager);
        _scope = new();
        _context = new(_correlationId, typeof(object), new object(), [], new(), ServiceProvider: _serviceProvider);
    }
}
