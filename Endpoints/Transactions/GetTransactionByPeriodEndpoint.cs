using System.Security.Claims;
using Dima.Api.Common.Api;
using Dima.Core;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Transaction;
using Dima.Core.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Dima.Api.Endpoints.Transactions;

public class GetTransactionByPeriodEndpoint : IEndpoint 
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapGet("/", HandleAsync)
            .WithName("Categories: GetAllTransactions")
            .WithSummary("Obtem todas transações")
            .WithDescription("Obtem todas transações")
            .WithOrder(5)
            .Produces<PagedResponse<List<Transaction>?>>();

    private static async Task<IResult> HandleAsync(
        ClaimsPrincipal user,
        ITransactionHandler handler,
        [FromQuery]DateTime? startDate = null,
        [FromQuery]DateTime? endDate = null,
        [FromQuery]int pageNumber = Configuration.DefaultPageNumber,
        [FromQuery]int pageSize = Configuration.DefaultPageSize) 
    {
        var request = new GetTransactionsByPeriodRequest
        {
            UserId = user.Identity?.Name ?? string.Empty,
            PageNumber = pageNumber,
            PageSize = pageSize,
            StartDate = startDate,
            EndDate = endDate
        };
            
        var result = await handler.GetByPeriodAsync(request);
        return result.IsSuccess
            ? Results.Ok(result)
            : Results.BadRequest(result);
    }
}