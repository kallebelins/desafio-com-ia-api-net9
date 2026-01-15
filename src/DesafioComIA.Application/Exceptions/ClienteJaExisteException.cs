using Mvp24Hours.Core.Exceptions;

namespace DesafioComIA.Application.Exceptions;

public class ClienteJaExisteException : BusinessException
{
    public ClienteJaExisteException(string mensagem) 
        : base(mensagem, "CLIENTE_JA_EXISTE")
    {
    }

    public ClienteJaExisteException(string mensagem, IDictionary<string, object>? context) 
        : base(mensagem, "CLIENTE_JA_EXISTE", context)
    {
    }
}
