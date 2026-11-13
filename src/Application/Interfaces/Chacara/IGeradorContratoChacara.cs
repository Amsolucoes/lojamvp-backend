using LojaApi.Domain.Chacara;
using LojaApi.Models;

namespace LojaApi.Application.Chacara;

// PORTA: o que o Application precisa do mundo externo, descrito como interface. Quem implementa mora no projeto da API
// (Infrastructure) e é ligado no registro do módulo. Assim o Application não depende de nenhum serviço externo e dá para
// trocar/simular cada um (os testes usam um Mercado Pago de mentira).

public interface IGeradorContratoChacara
{
    byte[] Gerar(Reserva reserva, Loja loja, InfoChacara? info, ConfiguracaoPrecoChacara cfg);
}
