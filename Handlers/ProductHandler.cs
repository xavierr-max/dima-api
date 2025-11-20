using Dima.Api.Data;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Orders;
using Dima.Core.Responses;
using Microsoft.EntityFrameworkCore;

namespace Dima.Api.Handlers;

public class ProductHandler(AppDbContext context) : IProductHandler
{
    //pega todos os produtos de maneira paginada
    public async Task<PagedResponse<List<Product>?>> GetAllAsync(GetAllProductsRequest request)
    {
        try
        {
            //busca os produtos ativos 
            var query = context
                .Products
                .AsNoTracking()
                .Where(x => x.IsActive == true)
                .OrderBy(x => x.Title);
            
            //paginacao
            var products = await query
                .Skip((request.PageNumber - 1) * request.PageSize) //0, ou seja nao pula nenhuma página
                .Take(request.PageSize)//25, pega os primeiros 25 elementos 
                .ToListAsync();// retorna como uma lista de Products
            
            var count = await query.CountAsync();

            return new PagedResponse<List<Product>?>(
                products,
                count,
                request.PageNumber,
                request.PageSize);
        }
        catch 
        {
            return new PagedResponse<List<Product>?>(null, 500, "Não foi possível consultar o produto.");
        }
    }

    //busca um produto pelo seu Slug 
    public async Task<Response<Product?>> GetBySlugAsync(GetProductBySlugRequest request)
    {
        try
        {
            //busca os produtos pelo Slug e que estao ativos
            var product = await context
                .Products
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Slug == request.Slug && x.IsActive == true);
            
            return product is null 
                ? new  Response<Product?>(null, 404, "Produto não encontrado")
                : new Response<Product?>(product);
            
        }
        catch 
        {
            return new Response<Product?>(null, 500, "Não foi possível recuperar o produto");
        }
    }
}