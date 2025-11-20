namespace Dima.Api;

//Atua como um contêiner de constantes e configurações globais para a aplicação backend
public static class ApiConfiguration
{
    public const string CorsPolicyName = "wasm";

    public static string StringApiKey { get; set; } =  string.Empty;
}