using Dima.Api.Data;
using Dima.Core.Enums;
using Dima.Core.Handlers;
using Dima.Core.Models.Reports;
using Dima.Core.Requests.Reports;
using Dima.Core.Responses;
using Microsoft.EntityFrameworkCore;

namespace Dima.Api.Handlers;

//consulta das views e lógica das operacoes 
public class ReportHandler(AppDbContext context) : IReportHandler
{
    //busca os valores de entradas e saidas nos meses em que houveram (grafico de linha)
    public async Task<Response<List<IncomesAndExpenses>?>> GetIncomesAndExpensesReportAsync(GetIncomesAndExpensesRequest request)
    {
        await Task.Delay(1280);
        try
        {
            //recebe a consulta vwGetIncomesAndExpenses por meio do IncomesAndExpenses
            var data = await context
                .IncomesAndExpenses
                .AsNoTracking()
                .Where(x => x.UserId == request.UserId)
                .OrderByDescending(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            //retorna como resposta do método o conteúdo da consulta
            return new Response<List<IncomesAndExpenses>?>(data);
        }
        catch
        {
            return new Response<List<IncomesAndExpenses>?>(null, 500, "Não foi possível obter as entradas e saídas");
        }
    }

    //busca os valores somados das entradas por categoria
    public async Task<Response<List<IncomesByCategory>?>> GetIncomesByCategoryReportAsync(GetIncomesByCategoryRequest request)
    {
        await Task.Delay(2180);
        try
        {
            var data = await context
                .IncomesByCategories
                .AsNoTracking()
                .Where(x => x.UserId == request.UserId)
                .OrderByDescending(x => x.Year)
                .ThenBy(x => x.Category)
                .ToListAsync();

            return new Response<List<IncomesByCategory>?>(data);
        }
        catch
        {
            return new Response<List<IncomesByCategory>?>(null, 500,
                "Não foi possível obter as entradas por categoria");
        }
    }

    //busca os valores somados das saidas por categoria
    public async Task<Response<List<ExpensesByCategory>?>> GetExpensesByCategoryReportAsync(GetExpensesByCategoryRequest request)
    {
        await Task.Delay(812);
        try
        {
            var data = await context
                .ExpensesByCategories
                .AsNoTracking()
                .Where(x => x.UserId == request.UserId)
                .OrderByDescending(x => x.Year)
                .ThenBy(x => x.Category)
                .ToListAsync();

            return new Response<List<ExpensesByCategory>?>(data);
        }
        catch
        {
            return new Response<List<ExpensesByCategory>?>(null, 500,
                "Não foi possível obter as entradas por categoria");
        }
    }

    //buscar o total de entradas e saídas do usuario para o mês em curso.
    public async Task<Response<FinancialSummary?>> GetFinancialSummaryReportAsync(GetFinancialSummaryRequest request)
    {
        await Task.Delay(3280);
        //define o primeiro dia do mes e ano atual
        var startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        try
        {
            var data = await context
                .Transactions
                .AsNoTracking()
                .Where(
                    //Garante que apenas as transações do usuário logado sejam consideradas.
                    x => x.UserId == request.UserId
                         //Inclui transações a partir do início do mês atual.
                         && x.PaidOrReceivedAt >= startDate
                         //Inclui transações até o momento atual.
                         && x.PaidOrReceivedAt <= DateTime.Now
                )
                //técnica para forçar o SQL a agregar todos os resultados filtrados em um único grupo
                .GroupBy(x => 1)
                .Select(x => new FinancialSummary(
                    request.UserId,
                    //Soma o Amount (valor) de todas as transações do grupo que são do tipo Deposit
                    x.Where(ty => ty.Type == ETransactionType.Deposit).Sum(t => t.Amount),
                    //Soma o Amount de todas as transações do grupo que são do tipo Withdraw
                    x.Where(ty => ty.Type == ETransactionType.Withdraw).Sum(t => t.Amount))
                )
                //é usado para retornar o resultado da agregação (GroupBy)
                .FirstOrDefaultAsync();

            //retorna o conteudo geral da query para o método
            return new Response<FinancialSummary?>(data);
        }
        catch
        {
            return new Response<FinancialSummary?>(null, 500,
                "Não foi possível obter o resultado financeiro");
        }
    }
}