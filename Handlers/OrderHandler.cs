using Dima.Api.Data;
using Dima.Core.Enums;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Orders;
using Dima.Core.Requests.Stripe;
using Dima.Core.Responses;
using Microsoft.EntityFrameworkCore;

namespace Dima.Api.Handlers;

public class OrderHandler(AppDbContext context, IStripeHandler stripeHandler) : IOrderHandler
{
    //busca o pedido do usuario logado e altera o estado para CANCELADO
    public async Task<Response<Order?>> CancelAsync(CancelOrderRequest request)
    {
        Order? order;
        try
        {
            //busca os pedidos pelo Id e do usuario logado, incluindo o Product e Voucher da Ordem
            order = await context
                .Orders
                .Include(x => x.Product)
                .Include(x => x.Voucher)
                .FirstOrDefaultAsync(x =>
                    x.Id == request.Id &&
                    x.UserId == request.UserId);

            if (order is null)
                return new Response<Order?>(null, 404, "Pedido não encontrado");
        }
        catch
        {
            return new Response<Order?>(null, 500, "Falha ao obter pedido");
        }

        //regra de negócio, somente pode ser cancelado se estiver aguardando pagamento
        switch (order.Status)
        {
            case EOrderStatus.Canceled:
                return new Response<Order?>(order, 400, "Este pedido já foi cancelado");

            case EOrderStatus.WaitingPayment:
                break;

            case EOrderStatus.Paid:
                return new Response<Order?>(order, 400, "Este pedido já foi pago e não pode ser cancelado");

            case EOrderStatus.Refunded:
                return new Response<Order?>(order, 400, "Este pedido já foi reemboldado e não pode ser cancelado");

            default:
                return new Response<Order?>(order, 400, "Este pedido já foi cancelado");
        }

        //define o pedido para CANCELADO
        order.Status = EOrderStatus.Canceled;
        //registra a hora do cancelamento do pedido
        order.UpdatedAt = DateTime.Now;

        //atualiza as alteracoes no banco
        try
        {
            context.Orders.Update(order);
            await context.SaveChangesAsync();
        }
        catch
        {
            return new Response<Order?>(order, 500, "Não foi possível cancelar seu pedido");
        }

        return new Response<Order?>(order, 200, $"Pedido {order.Number} foi cancelado");
    }

    //busca um produto e um voucher para criar um pedido para o usuario logado
    public async Task<Response<Order?>> CreateAsync(CreateOrderRequest request)
    {
        Product? product;
        try
        {
            //busca um produto ativo e pelo seu Id
            product = await context
                .Products
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == request.ProductId &&
                    x.IsActive == true);

            if (product is null)
                return new Response<Order?>(null, 400, "Produto não encontrado");

            //afirma para o banco que encontramos um Product já registrado e que vamos trabalhar com ele
            //para que o banco nao crie duplicata desse produto
            //"Anexar"
            context.Attach(product);
        }
        catch
        {
            return new Response<Order?>(null, 500, "Não foi possível obter o produto");
        }

        Voucher? voucher = null;
        try
        {
            //só vai bater no banco se o Voucher nao for nulo
            if (request.VoucherId is not null)
            {
                //busca o voucher ativo e pelo seu Id
                voucher = await context
                    .Vouchers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.Id == request.VoucherId &&
                        x.IsActive == true);

                //caso o voucher seja nulo
                if (voucher is null)
                    return new Response<Order?>(null, 400, "Voucher inválido ou não encontrado");

                //caso o voucher esteja inátivo
                if (voucher.IsActive == false)
                    return new Response<Order?>(null, 400, "Este voucher já foi utilizado");

                //define o voucher como inválido(usado)
                voucher.IsActive = false;
                //salva as alteracoes no banco
                context.Vouchers.Update(voucher);
            }
        }
        catch
        {
            return new Response<Order?>(null, 500, "Falha ao obter o voucher informado");
        }

        //cria um Pedido para o usuario logado
        var order = new Order
        {
            UserId = request.UserId, //recebe o usuário logado
            Product = product, //recebe o produto filtrado
            ProductId = request.ProductId, //recebe o id do produto
            Voucher = voucher, //recebe o voucher filtrado
            VoucherId = request.VoucherId //recebe o id do voucher
        };

        try
        {
            //adiciona o pedido ao banco em Orders
            await context.Orders.AddAsync(order);
            await context.SaveChangesAsync();
        }
        catch
        {
            return new Response<Order?>(null, 500, "Não foi possível realizar seu pedido");
        }

        return new Response<Order?>(order, 201, $"Pedido {order.Number} foi cadastrado com sucesso!");
    }

    //busca o pedido do usuario logado e altera o estado para PAGO
    public async Task<Response<Order?>> PaysAsync(PayOrderRequest request)
    {
        Order? order;
        try
        {
            //busca os pedidos do usuario logado
            order = await context
                .Orders
                .Include(x => x.Product)
                .Include(x => x.Voucher)
                .FirstOrDefaultAsync(x =>
                    x.Number == request.Number &&
                    x.UserId == request.UserId);

            if (order is null)
                return new Response<Order?>(null, 404, "Pedido não encontrado");
        }
        catch
        {
            return new Response<Order?>(null, 500, "Falha ao obter o pedido");
        }

        //regra de negócio
        switch (order.Status)
        {
            case EOrderStatus.Canceled:
                return new Response<Order?>(order, 400, "Este pedido já foi cancelado e não pode ser pago");

            case EOrderStatus.Paid:
                return new Response<Order?>(order, 400, "Este pedido já foi pago!");

            case EOrderStatus.Refunded:
                return new Response<Order?>(order, 400, "Este pedido já foi reembolsado e não pode ser pago");

            case EOrderStatus.WaitingPayment:
                break;

            default:
                return new Response<Order?>(order, 400, "Não foi possível pagar o pedido!");
        }

        try
        {
            var getTransactionsRequest = new GetTransactionsByOrderNumberRequest
            {
                Number = order.Number
            };
            var result = await stripeHandler.GetTransactionsByOrderNumberAsync(getTransactionsRequest);

            if (result.IsSuccess == false)
                return new Response<Order?>(null, 500, "Não foi possível localizar o pagamento do pedido");
            
            if(result.Data is null)
                return new Response<Order?>(null, 400, "Não foi possível localizar o pagamento do pedido");
                    
            if (result.Data.Any(x=> x.Refunded))
                return new Response<Order?>(null, 400, "Este pedido já teve o pagamento informado");
            
            if (result.Data.Any(x=> x.Paid))
                return new Response<Order?>(null, 400, "Este pedido não foi pago");

            request.ExternalReference = result.Data[0].Id;
        }
        //ERRO CRÍTICO: usuario pagou no stripe e nao foi possivel dar a baixa
        catch 
        {
            return new Response<Order?>(null, 400, "Não foi possível dar baixa no seu pedido");
        }
        
        //atualizacao do status do pedido
        order.Status = EOrderStatus.Paid;
        order.ExternalReference = request.ExternalReference;
        order.UpdatedAt = DateTime.Now;

        try
        {
            //atualiza no banco 
            context.Orders.Update(order);
            await context.SaveChangesAsync();
        }
        catch
        {
            return new Response<Order?>(null, 500, "Falha ao tentar pagar o pedido");
        }

        return new Response<Order?>(order, 200, $"Pedido {order.Number} foi pago com sucesso!");
    }

    //busca o pedido do usuario logado e altera o estado para ESTORNADO
    public async Task<Response<Order?>> RefundAsync(RefundOrderRequest request)
    {
        Order? order;
        try
        {
            //busca um pedido de um usuario logado pelo Id
            order = await context
                .Orders
                .Include(x => x.Product)
                .Include(x => x.Voucher)
                .FirstOrDefaultAsync(x =>
                    x.Id == request.Id &&
                    x.UserId == request.UserId);

            if (order is null)
                return new Response<Order?>(null, 404, "Pedido não encontrado");
        }
        catch
        {
            return new Response<Order?>(null, 500, "Não possível recuperar o seu pedido");
        }

        //Qualquer compra via internet tem (7 dias) corridos para arrependimento
        //Emissão de nota fiscal - (emitir somente após os 7 dias) NAO FOI FEITO PARA ESSA APLICACAO
        switch (order.Status)
        {
            case EOrderStatus.Canceled:
                return new Response<Order?>(order, 400, "Este pedido já foi cancelado e não pode ser estornado");

            case EOrderStatus.Paid:
                break;

            case EOrderStatus.Refunded:
                return new Response<Order?>(order, 400, "Este pedido já foi reembolsado");

            case EOrderStatus.WaitingPayment:
                return new Response<Order?>(order, 400, "O pedido ainda não foi pago e não pode ser reembolsado");

            default:
                return new Response<Order?>(order, 400, "Não foi possível reembolsar o pedido!");
        }

        order.Status = EOrderStatus.Refunded;
        order.UpdatedAt = DateTime.Now;

        try
        {
            context.Orders.Update(order);
            await context.SaveChangesAsync();
        }
        catch
        {
            return new Response<Order?>(order, 500, "Falha ao reembolsar o pagamento");
        }

        return new Response<Order?>(order, 200, $"Pedido {order.Number} foi estornado com sucesso!");
    }

    //lista todos os pedidos de modo paginado do usuario logado
    public async Task<PagedResponse<List<Order>?>> GetAllAsync(GetAllOrdersRequest request)
    {
        try
        {
            //recebe uma consulta dos pedidos criados pelo usuario logado
            var query = context
                .Orders
                .AsNoTracking()
                .Include(x => x.Product)
                .Include(x => x.Voucher)
                .Where(x => x.UserId == request.UserId)
                .OrderBy(x => x.CreatedAt);

            var orders = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var count = await query.CountAsync();

            return new PagedResponse<List<Order>?>(
                orders,
                count,
                request.PageNumber,
                request.PageSize);
        }
        catch
        {
            return new PagedResponse<List<Order>?>(null, 500, "Não foi possível obter seus pedidos");
        }
    }

    //busca o pedido do usuario logado pelo número do Id do pedido
    public async Task<Response<Order?>> GetByNumberAsync(GetOrderByNumberRequest request)
    {
        try
        {
            var order = await context
                .Orders
                .AsNoTracking()
                .Include(x => x.Product)
                .Include(x => x.Voucher)
                .FirstOrDefaultAsync(x =>
                    x.Number == request.Number &&
                    x.UserId == request.UserId);
            return order is null
                ? new Response<Order?>(null, 404, "Pedido não encontrado")
                : new Response<Order?>(order);
        }
        catch
        {
            return new Response<Order?>(null, 500, "Não foi possível recuperar seu pedido");
        }
    }
}