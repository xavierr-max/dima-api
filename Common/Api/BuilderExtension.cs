using Dima.Api.Data;
using Dima.Api.Handlers;
using Dima.Api.Models;
using Dima.Core;
using Dima.Core.Handlers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace Dima.Api.Common.Api;

public static class BuilderExtension
{
    public static void AddConfiguration(this WebApplicationBuilder builder)
    {
        Configuration.ConnectionString = builder
            .Configuration
            .GetConnectionString("DefaultConnection") ?? string.Empty;
        //propriedade de configuration recebe o valor do appsettings.json
        Configuration.BackendUrl = builder.Configuration.GetValue<string>("BackendUrl") ?? string.Empty;
        Configuration.FrontEndUrl = builder.Configuration.GetValue<string>("FrontEndUrl") ?? string.Empty;
        ApiConfiguration.StringApiKey = builder.Configuration.GetValue<string>("StripeApiKey") ?? string.Empty;
        
        StripeConfiguration.ApiKey = ApiConfiguration.StringApiKey;
    }

    public static void AddDocumentation(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        //CustomSchemaIds: define o nome completo da rota na documentacao
        builder.Services.AddSwaggerGen(x => { x.CustomSchemaIds(n => n.FullName); });
    }

    public static void AddSecurity(this WebApplicationBuilder builder)
    {
        //serviço middleware de autenticação que lida com o login e logout de usuários baseados em cookies de sessão.
        builder.Services
            .AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();
        
        builder.Services.AddAuthorization();
    }

    public static void AddDataContexts(this WebApplicationBuilder builder)
    {
        builder
            .Services
            .AddDbContext<AppDbContext>
                (x => { x.UseSqlServer(Configuration.ConnectionString); });
        
        //API do Identity
        builder.Services
                //representar os usuários no sistema
            .AddIdentityCore<User>()
                //representar as funções
            .AddRoles<IdentityRole<long>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddApiEndpoints();
    }

    //permitir que endereços diferentes(frontend) possa envia requisicoes 
    public static void AddCrossOrigin(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(
            options => options.AddPolicy(
                ApiConfiguration.CorsPolicyName,
                policy => policy
                        //Define quais domínios têm permissão para acessar a API
                    .WithOrigins([
                        Configuration.BackendUrl,
                        "https://icy-beach-0f027050f.3.azurestaticapps.net"
                    ])
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                ));
    }
    
    public static void AddServices(this WebApplicationBuilder builder)
    {
        //cria uma instancia do CategoryHandler por requisicao
        builder.Services.AddTransient<ICategoryHandler, CategoryHandler>();
        
        builder.Services.AddTransient<ITransactionHandler, TransactionHandler>();
        
        builder.Services.AddTransient<IProductHandler, ProductHandler>();
        
        builder.Services.AddTransient<IVoucherHandler, VoucherHandler>();
        
        builder.Services.AddTransient<IOrderHandler, OrderHandler>();
        
        builder.Services.AddTransient<IStripeHandler, StripeHandler>();
        
        builder.Services.AddTransient<IReportHandler, ReportHandler>();
    }
}