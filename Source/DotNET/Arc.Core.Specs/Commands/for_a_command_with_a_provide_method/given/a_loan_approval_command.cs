// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Commands.for_a_command_with_a_provide_method.given;

public class a_loan_approval_command : Specification
{
    public interface ICreditBureau
    {
        int GetScore(string applicant);
    }

    public record CreditScore(int Value);

    public record LoanApproved(string Applicant);

    [Command]
    public record ApproveLoan(string Applicant)
    {
        public CreditScore Provide(ICreditBureau bureau) => new(bureau.GetScore(Applicant));

        public LoanApproved? Handle(CreditScore creditScore) =>
            creditScore.Value >= 700 ? new LoanApproved(Applicant) : null;
    }
}
