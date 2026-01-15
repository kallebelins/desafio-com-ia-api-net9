using Mvp24Hours.Core.Exceptions;

namespace DesafioComIA.Application.Exceptions;

public class ClienteNaoEncontradoException : BusinessException
{
    public ClienteNaoEncontradoException(string mensagem) 
        : base(mensagem, "CLIENTE_NAO_ENCONTRADO")
    {
    }

    public ClienteNaoEncontradoException(string mensagem, IDictionary<string, object>? context) 
        : base(mensagem, "CLIENTE_NAO_ENCONTRADO", context)
    {
    }
}
