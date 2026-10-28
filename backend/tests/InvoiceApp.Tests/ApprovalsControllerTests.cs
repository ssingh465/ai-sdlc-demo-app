using InvoiceApp.Api.Auth;
using InvoiceApp.Api.Controllers;
using InvoiceApp.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalsControllerTests
{
    [Fact]
    public void Approve_UsesRoleFromSession()
    {
        var invoice = new Invoice
        {
            Id = "INV-1",
            CustomerName = "Test School",
            Amount = 72500m,
            Status = InvoiceStatus.PendingApproval
        };
        var (controller, repository) = ControllerFor(invoice, new UserSession("demo.manager", "Manager"));
        controller.Request.QueryString = new QueryString("?role=Director");
        controller.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("""{"role":"Director"}"""));
        controller.Request.ContentType = "application/json";
        var before = InvoiceSnapshot.Of(invoice);

        var result = controller.Approve("INV-1");

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal("Manager limit is 50,000; invoice is 72,500.",
            conflict.Value!.GetType().GetProperty("error")!.GetValue(conflict.Value));
        InvoiceSnapshot.AssertUnchanged(before, repository.GetById(invoice.Id));
        Assert.Equal(0, repository.UpdateCalls);
    }

    [Fact]
    public void Approve_AllowsDirectorSessionWithinDirectorLimit()
    {
        var invoice = new Invoice
        {
            Id = "INV-1",
            CustomerName = "Test School",
            Amount = 72500m,
            Status = InvoiceStatus.PendingApproval
        };
        var (controller, _) = ControllerFor(invoice, new UserSession("demo.director", "Director"));

        var result = controller.Approve("INV-1");

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(InvoiceStatus.Approved, invoice.Status);
        Assert.Equal("demo.director", invoice.DecidedBy);
    }

    [Fact]
    public void Approve_RefusesAdminSession()
    {
        var invoice = new Invoice
        {
            Id = "INV-1",
            CustomerName = "Test School",
            Amount = 100m,
            Status = InvoiceStatus.PendingApproval
        };
        var (controller, repository) = ControllerFor(invoice, new UserSession("demo.admin", "Admin"));
        var before = InvoiceSnapshot.Of(invoice);

        var result = controller.Approve("INV-1");

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal("Admin has no approval authority",
            conflict.Value!.GetType().GetProperty("error")!.GetValue(conflict.Value));
        InvoiceSnapshot.AssertUnchanged(before, repository.GetById(invoice.Id));
        Assert.Equal(0, repository.UpdateCalls);
    }

    [Fact]
    public void Approve_AcceptsNoClientSuppliedRole()
    {
        var parameters = typeof(ApprovalsController)
            .GetMethod(nameof(ApprovalsController.Approve))!
            .GetParameters();

        Assert.Collection(parameters, parameter => Assert.Equal("invoiceId", parameter.Name));
    }

    private static (ApprovalsController Controller, FakeInvoiceRepository Repository) ControllerFor(
        Invoice invoice,
        UserSession session)
    {
        var repository = new FakeInvoiceRepository(invoice);
        var controller = new ApprovalsController(new ApprovalService(repository))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.HttpContext.Items[HttpContextExtensions.SessionKey] = session;
        return (controller, repository);
    }
}
