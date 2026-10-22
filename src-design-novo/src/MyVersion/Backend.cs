// Backend.cs - a fronteira com o "controle virtual". Tudo o que fala com o driver
// fica atras desta interface (SPEC.md, secao 3 e risco R1).

using System;

namespace MyVersion
{
    public interface IControleVirtual : IDisposable
    {
        /// <summary>Nome do backend, para o log.</summary>
        string Nome { get; }

        /// <summary>Cria/apresenta o controle virtual ao sistema.</summary>
        void Conectar();

        /// <summary>Envia o estado completo (RF6).</summary>
        void Aplicar(EstadoControle estado);

        /// <summary>Remove o controle virtual do sistema.</summary>
        void Desconectar();
    }

    /// <summary>
    /// Backend "falso", sem driver: guarda o ultimo estado aplicado. Existe para poder testar
    /// o motor e o programa em maquina sem ViGEmBus instalado (e para os testes de logica).
    /// </summary>
    public sealed class ControleFalso : IControleVirtual
    {
        public EstadoControle Ultimo { get; private set; }
        public int Aplicacoes { get; private set; }
        public bool Conectado { get; private set; }

        public ControleFalso()
        {
            Ultimo = new EstadoControle();
        }

        public string Nome => "falso (sem driver)";

        public void Conectar()
        {
            Conectado = true;
            Ultimo = new EstadoControle();
            Aplicacoes = 0;
        }

        public void Aplicar(EstadoControle estado)
        {
            Ultimo = estado.Copia();
            Aplicacoes++;
        }

        public void Desconectar() => Conectado = false;

        public void Dispose()
        {
        }
    }
}
