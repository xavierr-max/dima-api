using System.Security.Claims;
using Dima.Api.Common.Api;
using Dima.Core.Models.Account;

namespace Dima.Api.Endpoints.Identity;

public class GetRolesEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapGet("/roles", HandleAsync)
            .RequireAuthorization();

    //método para obter as claims do usuário
    //ClaimsPrincipal: é o objeto que representa "quem é o usuário atual"
    private static Task<IResult> HandleAsync(ClaimsPrincipal user)
    {
        if (user.Identity is null || !user.Identity.IsAuthenticated)
            return Task.FromResult(Results.Unauthorized());
        
        //recebe somente as claims do usuário logado
        var identity = (ClaimsIdentity)user.Identity;
        //extrai as claims e cria um objeto RoleClaim
        var roles = identity
            .FindAll(identity.RoleClaimType)
            .Select(c => new RoleClaim
            {
                Issuer = c.Issuer,
                OriginalIssuer = c.OriginalIssuer,
                Type = c.Type,
                Value = c.Value,
                ValueType = c.ValueType
            });
        
        //retorna os claims do usuario logado
        return Task.FromResult(Results.Ok(roles));
    }
}