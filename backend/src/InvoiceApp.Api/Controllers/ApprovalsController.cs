using InvoiceApp.Api.Auth;
using InvoiceApp.Domain;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequireRole("Manager", "Director", "Admin")]
public class ApprovalsController : ApiControllerBase
{
    private readonly ApprovalService _approvals;

    public ApprovalsController(ApprovalService approvals)
    {
        _approvals = approvals;
    }

    public record RejectRequest(string Reason);

    [HttpPost("{invoiceId}/approve")]
    public ActionResult<Invoice> Approve(string invoiceId) =>
        Handle(() =>
        {
            var session = HttpContext.GetSession();
            var role = Enum.Parse<ApprovalRole>(session.Role);
            return _approvals.Approve(invoiceId, session.Username, role);
        });

    [HttpPost("{invoiceId}/reject")]
    public ActionResult<Invoice> Reject(string invoiceId, [FromBody] RejectRequest request) =>
        Handle(() => _approvals.Reject(invoiceId, HttpContext.GetSession().Username, request.Reason));
}
