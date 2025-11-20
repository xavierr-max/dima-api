using Dima.Core;
using Dima.Core.Handlers;
using Dima.Core.Requests.Stripe;
using Dima.Core.Responses;
using Dima.Core.Responses.Stripe;
using Stripe;
using Stripe.Checkout;

namespace Dima.Api.Handlers;

public class StripeHandler : IStripeHandler
{
    //cria uma sessao com base nos valores fornecidos de uma ordem e a chave secreta
    public async Task<Response<string?>> CreateSessionAsync(CreateSessionRequest request)
    {
        var options = new SessionCreateOptions
        {
            CustomerEmail = request.UserId,
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    { "order", request.OrderNumber }
                }
            },
            PaymentMethodTypes =
            [
                "card"
            ],
            LineItems =
            [
                new SessionLineItemOptions
                {
                   PriceData = new SessionLineItemPriceDataOptions
                   {
                       Currency = "BRL",
                       ProductData = new SessionLineItemPriceDataProductDataOptions
                       {
                           Name = request.ProductTitle,
                           Description = request.ProductDescription,
                       },
                       UnitAmount = request.OrderTotal,
                   },
                   Quantity = 1
                }
            ],
            Mode = "payment",
            SuccessUrl = $"{Configuration.FrontEndUrl}/pedidos/{request.OrderNumber}/confirmar",
            CancelUrl = $"{Configuration.FrontEndUrl}/pedidos/{request.OrderNumber}/cancelar"
        };

        var service = new SessionService();

        var session = await service.CreateAsync(options);

        return new Response<string?>(session.Id);
    }

    //busca os numeros das transacoes no Stripe para criar uma lista personalizada das informacoes das transacoes
    public async Task<Response<List<StripeTransactionResponse>>> GetTransactionsByOrderNumberAsync(
        GetTransactionsByOrderNumberRequest request)
    {
        var options = new ChargeSearchOptions
        {
            Query = $"metadata['order']: '{request.Number}' "
        };
        var service = new ChargeService();
        //recebe as transacoes
        var result = await service.SearchAsync(options);

        if (result.Data.Count == 0)
            return new Response<List<StripeTransactionResponse>>(null, 404, "Nenhuma transação encontrada");

        var data = new List<StripeTransactionResponse>();
        foreach (var item in result.Data)
        {
            data.Add(new StripeTransactionResponse
            {
                Id = item.Id,
                Email = item.BillingDetails.Email,
                Amount = item.Amount,
                AmountCaptured = item.AmountCaptured,
                Status = item.Status,
                Paid = item.Paid,
                Refunded = item.Refunded,
            });
        }

        return new Response<List<StripeTransactionResponse>>(data);

    }
}