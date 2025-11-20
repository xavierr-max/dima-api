using Microsoft.AspNetCore.Identity;

namespace Dima.Api.Models;

//entidade de usuário para o sistema de segurança ASP.NET Core Identity
public class User : IdentityUser<long>
{
    public List<IdentityRole<long>>? Roles { get; set; }
}