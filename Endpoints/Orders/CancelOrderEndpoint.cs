using System.Security.Claims;
using Azure;
using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Orders;

namespace Dima.Api.Endpoints.Orders;

public class CancelOrderEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapPost("/{id}/cancel", HandleAsync)
            .WithName("Orders: Cancel order")
            .WithSummary("Cancela um pedido")
            .WithDescription("Cancela um pedido")
            .WithOrder(4)
            .Produces<Response<Order?>>();

    private static async Task<IResult> HandleAsync(
        IOrderHandler handler,
        long id, //vem da url
        ClaimsPrincipal user)
    {
        var request = new CancelOrderRequest
        {
            Id = id,
            UserId = user.Identity!.Name ?? string.Empty,
        };

        var result = await handler.CancelAsync(request);
        return result.IsSuccess
            ? TypedResults.Ok(result)
            : TypedResults.BadRequest(result);
    }
}