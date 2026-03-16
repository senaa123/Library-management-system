using LibraryM.Application.Transactions;
using LibraryM.Application.Transactions.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryM.WebApi.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class TransactionsController : ApiControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTransactions([FromQuery] TransactionQueryRequest request, CancellationToken cancellationToken)
    {
        var result = await _transactionService.GetTransactionsAsync(request, GetCurrentUserId(), GetCurrentUserRole(), cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToFailureResult(result);
    }
}
