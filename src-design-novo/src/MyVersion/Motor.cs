// Motor.cs - modelo de estado do controle virtual (secao 6) e o motor de mapeamento
// (secoes 8.1 e 8.2 do SPEC.md).

using System;
using System.Collections.Generic;
using System.Text;

namespace MyVersion
{
    /// <summary>Estado completo do controle virtual: 14 botoes + 4 sticks (16 bits com sinal) + 2 gatilhos (8 bits).</summary>
    public sealed class EstadoControle
    {
        public readonly HashSet<Botao> Botoes = new HashSet<Botao>();

        public int LX, LY, RX, RY;   // -32768 .. 32767
        public int LT, RT;           // 0 .. 255

        public void Zerar()
        {
            Botoes.Clear();
            LX = LY = RX = RY = 0;
            LT = RT = 0;
        }

        public EstadoControle Copia()
        {
            EstadoControle c = new EstadoControle { LX = LX, LY = LY, RX = RX, RY = RY, LT = LT, RT = RT };
            foreach (Botao b in Botoes) c.Botoes.Add(b);
            return c;
        }

        public bool Igual(EstadoControle o)
        {
            if (o == null) return false;
            if (LX != o.LX || LY != o.LY || RX != o.RX || RY != o.RY || LT != o.LT || RT != o.RT) return false;
            if (Botoes.Count != o.Botoes.Count) return false;
            foreach (Botao b in Botoes) if (!o.Botoes.Contains(b)) return false;
            return true;
        }

        public int Valor(Eixo e)
        {
            switch (e)
            {
                case Eixo.LX: return LX;
                case Eixo.LY: return LY;
                case Eixo.RX: return RX;
                case Eixo.RY: return RY;
                case Eixo.LT: return LT;
                case Eixo.RT: return RT;
                default: return 0;
            }
        }

        public void Definir(Eixo e, int valor)
        {
            switch (e)
            {
                case Eixo.LX: LX = valor; break;
                case Eixo.LY: LY = valor; break;
                case Eixo.RX: RX = valor; break;
                case Eixo.RY: RY = valor; break;
                case Eixo.LT: LT = valor; break;
                case Eixo.RT: RT = valor; break;
            }
        }

        public string Descrever()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("botoes=[");
            List<Botao> ordenados = new List<Botao>(Botoes);
            ordenados.Sort();
            for (int i = 0; i < ordenados.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(ordenados[i]);
            }
            sb.Append("] LX=").Append(LX).Append(" LY=").Append(LY);
            sb.Append(" RX=").Append(RX).Append(" RY=").Append(RY);
            sb.Append(" LT=").Append(LT).Append(" RT=").Append(RT);
            return sb.ToString();
        }
    }

    /// <summary>
    /// Motor de mapeamento. Nao conhece o backend: recebe eventos de tecla e devolve o estado
    /// completo do controle a cada tique.
    /// </summary>
    public sealed class Motor
    {
        private readonly ArquivoPerfis _arquivo;
        private Perfil _perfil;
        private readonly HashSet<int> _pressionadas = new HashSet<int>();
        private readonly HashSet<int> _consumidas = new HashSet<int>();
        private readonly Dictionary<string, bool> _combosArmados = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Eixo, double> _atual = new Dictionary<Eixo, double>();
        // o hook de teclado roda numa thread e o laco de atualizacao em outra: tudo aqui e travado
        private readonly object _trava = new object();

        /// <summary>Avisos e eventos para o log (troca de perfil, atalho recusado...).</summary>
        public event Action<string> Mensagem;

        public string PerfilAtivo => _perfil.Nome;

        public ArquivoPerfis Arquivo => _arquivo;

        public Motor(ArquivoPerfis arquivo, string perfilInicial = null)
        {
            if (arquivo == null) throw new ArgumentNullException(nameof(arquivo));
            _arquivo = arquivo;
            string nome = perfilInicial ?? arquivo.PerfilInicial;
            if (!arquivo.Perfis.TryGetValue(nome, out Perfil p))
                throw new PerfilInvalidoException($"perfil '{nome}' nao existe");
            _perfil = p;
            ZerarEixos();
        }

        private static int Chave(CodigoTecla t) => t.Chave;

        public void Apertar(int scan, bool estendida)
        {
            int k = new CodigoTecla(scan, estendida).Chave;
            lock (_trava) _pressionadas.Add(k);
        }

        public void Soltar(int scan, bool estendida)
        {
            int k = new CodigoTecla(scan, estendida).Chave;
            lock (_trava)
            {
                _pressionadas.Remove(k);
                _consumidas.Remove(k);
            }
        }

        public void SoltarTudo()
        {
            lock (_trava)
            {
                _pressionadas.Clear();
                _consumidas.Clear();
            }
        }

        /// <summary>Quantas teclas estao pressionadas agora (util para diagnostico).</summary>
        public int TeclasPressionadas
        {
            get { lock (_trava) return _pressionadas.Count; }
        }

        /// <summary>
        /// Um tique do motor (ordem da secao 8.1). Devolve o estado completo ja com rampa,
        /// zona morta e limites aplicados.
        /// </summary>
        public EstadoControle Tique(double msDecorridos)
        {
            lock (_trava) return TiqueSemTrava(msDecorridos);
        }

        private EstadoControle TiqueSemTrava(double msDecorridos)
        {
            bool trocou = ProcessarAtalhos();

            EstadoControle estado = new EstadoControle();

            if (trocou)
            {
                // secao 8.1: a troca de perfil zera o estado e nao segue para a aplicacao dos mapeamentos
                return estado;
            }

            // 3. aplica os mapeamentos do perfil ativo
            foreach (Mapeamento m in _perfil.Mapeamentos)
            {
                if (m.Tipo != TipoMapeamento.Botao) continue;
                int k = Chave(m.Tecla);
                if (_pressionadas.Contains(k) && !_consumidas.Contains(k)) estado.Botoes.Add(m.AlvoBotao);
            }

            foreach (Eixo eixo in Enum.GetValues<Eixo>())
            {
                double alvo = 0.0;
                foreach (Mapeamento m in _perfil.Mapeamentos)
                {
                    if (m.Tipo != TipoMapeamento.Eixo || m.AlvoEixo != eixo) continue;
                    int k = Chave(m.Tecla);
                    if (_pressionadas.Contains(k) && !_consumidas.Contains(k)) alvo += m.Sentido * m.ValorMaximo;
                }

                double limiteSuperior = Faixas.Maximo(eixo);
                double limiteInferior = Faixas.Minimo(eixo);
                double alvoLimitado = Math.Max(limiteInferior, Math.Min(limiteSuperior, alvo));

                double atual = _atual[eixo];
                // "voltando" = o alvo esta mais perto do repouso do que o valor atual; nesse caso vale a
                // rampa de VOLTA (rampa_volta_ms). Na ida vale a rampa normal (rampa_ms).
                bool voltando = Math.Abs(alvoLimitado) < Math.Abs(atual);
                double taxa = TaxaDoEixo(eixo, voltando);   // unidades por milissegundo
                if (double.IsPositiveInfinity(taxa))
                {
                    atual = alvoLimitado;                   // sem rampa (0) = vai/volta no mesmo tique
                }
                else
                {
                    double passo = taxa * Math.Max(0.0, msDecorridos);
                    if (Math.Abs(alvoLimitado - atual) <= passo) atual = alvoLimitado;
                    else atual += Math.Sign(alvoLimitado - atual) * passo;
                }

                // Guarda a rampa REAL, sem zona morta: a zona morta e um filtro de SAIDA
                // (secao 8.1, passo 4). Guardar o valor JA filtrado impediria a rampa de
                // atravessar a zona morta quando o passo do tique e menor que ela.
                _atual[eixo] = atual;

                // 4. zona morta de saida e limites
                int inteiro = (int)Math.Round(atual);
                int zona = ZonaMortaDoEixo(eixo);
                if (zona > 0 && Math.Abs(inteiro) < zona) inteiro = 0;
                inteiro = Math.Max((int)limiteInferior, Math.Min((int)limiteSuperior, inteiro));

                estado.Definir(eixo, inteiro);
            }

            return estado;
        }

        private double TaxaDoEixo(Eixo eixo, bool volta)
        {
            double taxa = 0.0;
            foreach (Mapeamento m in _perfil.Mapeamentos)
            {
                if (m.Tipo != TipoMapeamento.Eixo || m.AlvoEixo != eixo) continue;
                int rampa = volta ? (m.RampaVoltaMs < 0 ? m.RampaMs : m.RampaVoltaMs) : m.RampaMs;
                if (rampa == 0) return double.PositiveInfinity;    // sem rampa: vai (ou volta) no mesmo tique
                double t = (double)m.ValorMaximo / rampa;
                if (t > taxa) taxa = t;
            }
            return taxa;
        }

        /// <summary>
        /// Zona morta NA ESCALA DO EIXO. O valor do perfil e dado na escala dos sticks (32767) e e
        /// convertido proporcionalmente para os gatilhos (0..255).
        /// Defeito real medido em uso em 2026-10-18 (relato do usuario: "o perfil dois nao tem as
        /// teclas de aceleracao"): com zona_morta 3000, que esta na escala dos sticks, o gatilho RT
        /// ficava SEMPRE em 0, porque todo valor de gatilho (0..255) tem modulo menor que 3000.
        /// </summary>
        private int ZonaMortaDoEixo(Eixo eixo)
        {
            if (_perfil.ZonaMorta <= 0) return 0;
            if (!Faixas.EhGatilho(eixo)) return _perfil.ZonaMorta;
            return (int)Math.Round(_perfil.ZonaMorta * 255.0 / 32767.0);
        }

        private void ZerarEixos()
        {
            _atual.Clear();
            foreach (Eixo e in Enum.GetValues<Eixo>()) _atual[e] = 0.0;
        }

        /// <summary>Passo 2 da secao 8.1: um atalho completo troca de perfil e consome as teclas dele.</summary>
        private bool ProcessarAtalhos()
        {
            bool disparou = false;
            foreach (Atalho at in _perfil.Atalhos)
            {
                bool completo = true;
                foreach (CodigoTecla t in at.Teclas) if (!_pressionadas.Contains(Chave(t))) { completo = false; break; }

                _combosArmados.TryGetValue(at.Combinacao, out bool armado);
                if (completo && !armado)
                {
                    _combosArmados[at.Combinacao] = true;
                    foreach (CodigoTecla t in at.Teclas) _consumidas.Add(Chave(t));
                    TrocarPerfil(at.PerfilDestino);
                    disparou = true;
                }
                else if (!completo && armado)
                {
                    _combosArmados[at.Combinacao] = false;
                }
            }
            return disparou;
        }

        public void TrocarPerfil(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome) || !_arquivo.Perfis.TryGetValue(nome, out Perfil novo))
            {
                Emitir($"aviso: perfil '{nome}' nao existe; mantendo '{_perfil.Nome}'");
                return;
            }
            string antigo = _perfil.Nome;
            if (string.Equals(antigo, novo.Nome, StringComparison.OrdinalIgnoreCase)) return;

            _perfil = novo;
            ZerarEixos();                 // evita "prender" botao/eixo do perfil anterior (RF5)
            // NAO limpamos _consumidas: as teclas do atalho continuam consumidas enquanto
            // estiverem pressionadas (secao 8.4.4 do SPEC.md)
            Emitir($"perfil trocado: {antigo} -> {novo.Nome}");
        }

        private void Emitir(string texto)
        {
            Mensagem?.Invoke(texto);
        }
    }
}
