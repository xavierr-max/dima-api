using Dima.Api.Data;
using Dima.Core.Common.Extensions;
using Dima.Core.Enums;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Categories;
using Dima.Core.Requests.Transaction;
using Dima.Core.Responses;
using Microsoft.EntityFrameworkCore;

namespace Dima.Api.Handlers;

public class TransactionHandler(AppDbContext context) : ITransactionHandler
{
    public async Task<Response<Transaction?>> CreateAsync(CreateTransactionRequest request)
    {
        //caso o tipo da transacao seja withdraw(saída), o valor será registrado como negativo
        if (request is { Type: ETransactionType.Withdraw, Amount: >= 0 })
            request.Amount *= -1;
        try
        {
            var transaction = new Transaction
            {
                UserId = request.UserId,
                CategoryId = request.CategoryId,
                CreatedAt = DateTime.Now,
                Amount = request.Amount,
                PaidOrReceivedAt = request.PaidOrReceivedAt,
                Title = request.Title,
                Type = request.Type,
            };

            await context.Transactions.AddAsync(transaction);
            await context.SaveChangesAsync();

            return new Response<Transaction?>(transaction, 201, "transação criada com sucesso");
        }
        catch
        {
            return new Response<Transaction?>(null, 500,
                "Falha interna no servidor: Não foi possível criar sua transação");
        }
    }

    public async Task<Response<Transaction?>> UpdateAsync(UpdateTransactionRequest request)
    {
        //caso o tipo da transacao seja withdraw(saída), o valor será registrado como negativo
        if (request is { Type: ETransactionType.Withdraw, Amount: >= 0 })
            request.Amount *= -1;
        
        try
        {
            var transaction = await context
                .Transactions
                .FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == request.UserId);

            if (transaction is null)
                return new Response<Transaction?>(null, 404,
                    "Transação não encontrada");
            
            transaction.CategoryId = request.CategoryId;
            transaction.Amount = request.Amount;
            transaction.Title = request.Title;
            transaction.Type = request.Type;
            transaction.PaidOrReceivedAt = request.PaidOrReceivedAt;
            
            context.Transactions.Update(transaction);
            await context.SaveChangesAsync();

            return new Response<Transaction?>(transaction);
        }
        catch 
        {
            return new Response<Transaction?>(null, 500, 
                "Falha interna no servidor: Não foi possível atualizar sua transação");
        }
    }

    public async Task<Response<Transaction?>> DeleteAsync(DeleteTransactionRequest request)
    {
        try
        {
            var transaction = await context
                .Transactions
                .FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == request.UserId);

            if (transaction is null)
                return new Response<Transaction?>(null, 404,
                    "Transação não encontrada");
            
            context.Transactions.Remove(transaction);
            await context.SaveChangesAsync();

            return new Response<Transaction?>(transaction);
        }
        catch 
        {
            return new Response<Transaction?>(null, 500, 
                "Falha interna no servidor: Não foi possível deletar sua transação");
        }
    }

    public async Task<Response<Transaction?>> GetByIdAsync(GetTransactionByIdRequest request)
    {
        try
        {
            var transaction = await context
                .Transactions
                .FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == request.UserId);

            if (transaction is null)
                return new Response<Transaction?>(null, 404,
                    "Transação não encontrada");

            return new Response<Transaction?>(transaction);
        }
        catch 
        {
            return new Response<Transaction?>(null, 500, 
                "Falha interna no servidor: Não foi possível obter sua transação");
        }
    }

    public async Task<PagedResponse<List<Transaction>?>> GetByPeriodAsync(GetTransactionsByPeriodRequest request)
    {
        try
        {
            //recebe o primeiro dia do mes ou null
            request.StartDate ??= DateTime.Now.GetFirstDay();
            //recebe o ultimo dia do mes ou null
            request.EndDate ??= DateTime.Now.GetLastDay();
        }
        catch
        {
            return new PagedResponse<List<Transaction>?>(null, 500,
                "Falha interna no servidor: Não foi possível obter a data de início ou término");
        }
        
        try
        {
            //query(consulta) que filtra dados em um periodo de tempo e de um usuario em especifico, ordenado por titulo
            var query = context
                .Transactions
                .AsNoTracking()
                //filtra os valores que estiverem entre a data StartDate e EndDate e devem pertencer ao usuario logado
                .Where(x =>
                    x.PaidOrReceivedAt >= request.StartDate &&
                    x.PaidOrReceivedAt <= request.EndDate &&
                    x.UserId == request.UserId)
                .OrderBy(x => x.PaidOrReceivedAt);

            //pega a consulta filtrada (query) e aplica a lógica de paginação nela.
            var transactions = await query 
                .Skip(request.PageSize * (request.PageNumber - 1))
                .Take(request.PageSize)
                .ToListAsync(); 

            //responsável por contar o total de registros
            var count = await query.CountAsync(); 

            return new PagedResponse<List<Transaction>?>(
                transactions, count, request.PageNumber, request.PageSize);
        }
        catch 
        {
            return new PagedResponse<List<Transaction>?>(null, 500,
                "Falha interna no servidor: Não foi possível obter as transações");
        }
    }
}