using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalPolicyTests
{
    [Fact]
    public void LimitFor_Manager_Returns50000() =>
        Assert.Equal(50000m, ApprovalPolicy.LimitFor(ApprovalRole.Manager));

    [Fact]
    public void LimitFor_Director_Returns500000() =>
        Assert.Equal(500000m, ApprovalPolicy.LimitFor(ApprovalRole.Director));

    [Fact]
    public void TwoApproverThreshold_ReturnsDirectorLimit() =>
        Assert.Equal(ApprovalPolicy.LimitFor(ApprovalRole.Director), ApprovalPolicy.TwoApproverThreshold);

    [Fact]
    public void LimitFor_Admin_ReturnsNull() =>
        Assert.Null(ApprovalPolicy.LimitFor(ApprovalRole.Admin));
}
