// Perfil.cs - modelo do arquivo de perfis (JSON) e a validacao da secao 7 do SPEC.md.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace MyVersion
{
    public enum TipoMapeamento { Botao, Eixo }

    /// <summary>Os 14 botoes da secao 6 do SPEC.md.</summary>
    public enum Botao { A, B, X, Y, LB, RB, BACK, START, LS, RS, UP, DOWN, LEFT, RIGHT }

    /// <summary>Os 6 eixos da secao 6 do SPEC.md.</summary>
    public enum Eixo { LX, LY, RX, RY, LT, RT }

    public static class Faixas
    {
        public static bool EhGatilho(Eixo e) => e == Eixo.LT || e == Eixo.RT;

        public static int Minimo(Eixo e) => EhGatilho(e) ? 0 : -32768;

        public static int Maximo(Eixo e) => EhGatilho(e) ? 255 : 32767;

        public static int ValorMaximoPadrao(Eixo e) => EhGatilho(e) ? 255 : 32767;
    }

    public sealed class Mapeamento
    {
        public string TeclaNome;
        public CodigoTecla Tecla;
        public TipoMapeamento Tipo;
        public Botao AlvoBotao;
        public Eixo AlvoEixo;
        public int Sentido = 1;
        public int RampaMs = 150;
        /// <summary>Rampa de VOLTA ao repouso. -1 = usar a mesma de ida; 0 = volta imediata.</summary>
        public int RampaVoltaMs = -1;
        public int ValorMaximo;
    }

    public sealed class Atalho
    {
        public string Combinacao;
        public List<CodigoTecla> Teclas = new List<CodigoTecla>();
        public string PerfilDestino;
    }

    public sealed class Perfil
    {
        public string Nome;
        public string Descricao = "";
        public List<Mapeamento> Mapeamentos = new List<Mapeamento>();
        public int ZonaMorta;
        public List<Atalho> Atalhos = new List<Atalho>();
    }

    public sealed class ArquivoPerfis
    {
        public int Versao;
        public string PerfilInicial;
        public int TiqueMs = 8;
        public Dictionary<string, Perfil> Perfis = new Dictionary<string, Perfil>(StringComparer.OrdinalIgnoreCase);
        public string Caminho;
    }

    public sealed class PerfilInvalidoException : Exception
    {
        public PerfilInvalidoException(string mensagem) : base(mensagem) { }
    }

    public static class CarregadorPerfil
    {
        public static ArquivoPerfis Carregar(string caminho, out List<string> avisos)
        {
            avisos = new List<string>();
            if (string.IsNullOrWhiteSpace(caminho)) throw new PerfilInvalidoException("caminho do arquivo de perfil vazio");
            if (!File.Exists(caminho)) throw new PerfilInvalidoException("arquivo de perfil nao encontrado: " + caminho);

            string texto;
            try { texto = File.ReadAllText(caminho); }
            catch (Exception e) { throw new PerfilInvalidoException("nao consegui ler o arquivo de perfil: " + e.Message); }

            JsonDocument doc;
            try { doc = JsonDocument.Parse(texto, new JsonDocumentOptions { AllowTrailingCommas = false, CommentHandling = JsonCommentHandling.Skip }); }
            catch (JsonException e) { throw new PerfilInvalidoException("JSON invalido: " + e.Message); }

            using (doc)
            {
                JsonElement raiz = doc.RootElement;
                if (raiz.ValueKind != JsonValueKind.Object) throw new PerfilInvalidoException("a raiz do arquivo precisa ser um objeto JSON");

                ArquivoPerfis arq = new ArquivoPerfis { Caminho = caminho };
                arq.Versao = LerInteiro(raiz, "versao", obrigatorio: true, padrao: 0, "raiz");
                if (arq.Versao != 1) throw new PerfilInvalidoException($"versao {arq.Versao} nao suportada (esta versao entende a versao 1)");

                arq.TiqueMs = LerInteiro(raiz, "tique_ms", obrigatorio: false, padrao: 8, "raiz");
                if (arq.TiqueMs < 1) throw new PerfilInvalidoException("tique_ms precisa ser maior que zero");

                if (!raiz.TryGetProperty("perfis", out JsonElement perfis) || perfis.ValueKind != JsonValueKind.Object)
                    throw new PerfilInvalidoException("falta o objeto 'perfis'");
                bool vazio = true;
                foreach (JsonProperty _ in perfis.EnumerateObject()) { vazio = false; break; }
                if (vazio) throw new PerfilInvalidoException("'perfis' esta vazio");

                foreach (JsonProperty p in perfis.EnumerateObject())
                {
                    Perfil perfil = new Perfil { Nome = p.Name };
                    JsonElement corpo = p.Value;
                    if (corpo.ValueKind != JsonValueKind.Object) throw new PerfilInvalidoException($"perfil '{p.Name}' precisa ser um objeto");

                    if (corpo.TryGetProperty("descricao", out JsonElement desc) && desc.ValueKind == JsonValueKind.String)
                        perfil.Descricao = desc.GetString();

                    perfil.ZonaMorta = LerInteiro(corpo, "zona_morta", false, 0, $"perfil '{p.Name}'");
                    if (perfil.ZonaMorta < 0) throw new PerfilInvalidoException($"perfil '{p.Name}': zona_morta nao pode ser negativa");

                    if (!corpo.TryGetProperty("mapeamentos", out JsonElement maps) || maps.ValueKind != JsonValueKind.Array)
                        throw new PerfilInvalidoException($"perfil '{p.Name}': falta o vetor 'mapeamentos'");

                    int indice = 0;
                    foreach (JsonElement m in maps.EnumerateArray())
                    {
                        string onde = $"perfil '{p.Name}', mapeamento #{indice}";
                        perfil.Mapeamentos.Add(LerMapeamento(m, onde, avisos));
                        indice++;
                    }

                    if (corpo.TryGetProperty("atalhos", out JsonElement atalhos) && atalhos.ValueKind == JsonValueKind.Array)
                    {
                        int ia = 0;
                        foreach (JsonElement a in atalhos.EnumerateArray())
                        {
                            perfil.Atalhos.Add(LerAtalho(a, $"perfil '{p.Name}', atalho #{ia}"));
                            ia++;
                        }
                    }

                    arq.Perfis[p.Name] = perfil;
                }

                if (!raiz.TryGetProperty("perfil_inicial", out JsonElement inicial) || inicial.ValueKind != JsonValueKind.String)
                    throw new PerfilInvalidoException("falta 'perfil_inicial'");
                arq.PerfilInicial = inicial.GetString();
                if (!arq.Perfis.ContainsKey(arq.PerfilInicial))
                    throw new PerfilInvalidoException($"perfil_inicial '{arq.PerfilInicial}' nao existe em 'perfis'");

                // todo perfil citado em atalho precisa existir
                foreach (Perfil perfil in arq.Perfis.Values)
                {
                    foreach (Atalho at in perfil.Atalhos)
                    {
                        if (!arq.Perfis.ContainsKey(at.PerfilDestino))
                            throw new PerfilInvalidoException($"atalho '{at.Combinacao}' do perfil '{perfil.Nome}' aponta para perfil inexistente '{at.PerfilDestino}'");
                    }
                }

                return arq;
            }
        }

        private static Mapeamento LerMapeamento(JsonElement m, string onde, List<string> avisos)
        {
            if (m.ValueKind != JsonValueKind.Object) throw new PerfilInvalidoException($"{onde}: precisa ser um objeto");

            if (!m.TryGetProperty("tecla", out JsonElement tecla) || tecla.ValueKind != JsonValueKind.String)
                throw new PerfilInvalidoException($"{onde}: falta 'tecla'");
            string nomeTecla = tecla.GetString();
            if (!TabelaTeclas.TentarResolver(nomeTecla, out CodigoTecla codigo))
                throw new PerfilInvalidoException($"{onde}: tecla desconhecida '{nomeTecla}'");

            if (!m.TryGetProperty("tipo", out JsonElement tipo) || tipo.ValueKind != JsonValueKind.String)
                throw new PerfilInvalidoException($"{onde}: falta 'tipo'");
            string nomeTipo = tipo.GetString();

            Mapeamento map = new Mapeamento { TeclaNome = TabelaTeclas.Canonico(nomeTecla) ?? nomeTecla, Tecla = codigo };

            if (string.Equals(nomeTipo, "botao", StringComparison.OrdinalIgnoreCase))
            {
                map.Tipo = TipoMapeamento.Botao;
                if (!m.TryGetProperty("alvo", out JsonElement alvo) || alvo.ValueKind != JsonValueKind.String)
                    throw new PerfilInvalidoException($"{onde}: falta 'alvo'");
                string nomeAlvo = alvo.GetString();
                if (!Enum.TryParse(nomeAlvo, true, out Botao b))
                    throw new PerfilInvalidoException($"{onde}: alvo de botao invalido '{nomeAlvo}' (aceitos: A, B, X, Y, LB, RB, BACK, START, LS, RS, UP, DOWN, LEFT, RIGHT)");
                map.AlvoBotao = b;
                return map;
            }

            if (string.Equals(nomeTipo, "eixo", StringComparison.OrdinalIgnoreCase))
            {
                map.Tipo = TipoMapeamento.Eixo;
                if (!m.TryGetProperty("alvo", out JsonElement alvo) || alvo.ValueKind != JsonValueKind.String)
                    throw new PerfilInvalidoException($"{onde}: falta 'alvo'");
                string nomeAlvo = alvo.GetString();
                if (!Enum.TryParse(nomeAlvo, true, out Eixo e))
                    throw new PerfilInvalidoException($"{onde}: alvo de eixo invalido '{nomeAlvo}' (aceitos: LX, LY, RX, RY, LT, RT)");
                map.AlvoEixo = e;

                map.Sentido = LerInteiro(m, "sentido", false, 1, onde);
                if (map.Sentido != 1 && map.Sentido != -1)
                    throw new PerfilInvalidoException($"{onde}: 'sentido' precisa ser +1 ou -1");
                if (Faixas.EhGatilho(e) && map.Sentido != 1)
                    throw new PerfilInvalidoException($"{onde}: gatilho {e} so aceita sentido +1");

                map.RampaMs = LerInteiro(m, "rampa_ms", false, 150, onde);
                if (map.RampaMs < 0) throw new PerfilInvalidoException($"{onde}: 'rampa_ms' nao pode ser negativo (0 = sem rampa, resposta no mesmo tique)");

                map.RampaVoltaMs = LerInteiro(m, "rampa_volta_ms", false, -1, onde);
                if (map.RampaVoltaMs < -1) throw new PerfilInvalidoException($"{onde}: 'rampa_volta_ms' nao pode ser negativo (0 = volta imediata, ausente = igual a rampa_ms)");

                map.ValorMaximo = LerInteiro(m, "valor_maximo", false, Faixas.ValorMaximoPadrao(e), onde);
                int teto = Faixas.EhGatilho(e) ? 255 : 32767;
                if (map.ValorMaximo < 1 || map.ValorMaximo > teto)
                    throw new PerfilInvalidoException($"{onde}: 'valor_maximo' precisa ficar entre 1 e {teto} para o eixo {e}");
                return map;
            }

            throw new PerfilInvalidoException($"{onde}: 'tipo' invalido '{nomeTipo}' (aceitos: botao, eixo)");
        }

        private static Atalho LerAtalho(JsonElement a, string onde)
        {
            if (a.ValueKind != JsonValueKind.Object) throw new PerfilInvalidoException($"{onde}: precisa ser um objeto");

            if (!a.TryGetProperty("combinacao", out JsonElement comb) || comb.ValueKind != JsonValueKind.String)
                throw new PerfilInvalidoException($"{onde}: falta 'combinacao'");
            string texto = comb.GetString();

            Atalho at = new Atalho { Combinacao = texto };
            foreach (string parte in texto.Split('+'))
            {
                string pedaco = parte.Trim();
                if (pedaco.Length == 0) throw new PerfilInvalidoException($"{onde}: combinacao '{texto}' tem uma parte vazia");
                if (!TabelaTeclas.TentarResolver(pedaco, out CodigoTecla codigo))
                    throw new PerfilInvalidoException($"{onde}: combinacao '{texto}': tecla desconhecida '{pedaco}'");
                if (!at.Teclas.Contains(codigo)) at.Teclas.Add(codigo);
            }
            if (at.Teclas.Count == 0) throw new PerfilInvalidoException($"{onde}: combinacao '{texto}' sem teclas");

            if (!a.TryGetProperty("perfil", out JsonElement destino) || destino.ValueKind != JsonValueKind.String)
                throw new PerfilInvalidoException($"{onde}: falta 'perfil'");
            at.PerfilDestino = destino.GetString();
            return at;
        }

        private static int LerInteiro(JsonElement obj, string campo, bool obrigatorio, int padrao, string onde)
        {
            if (!obj.TryGetProperty(campo, out JsonElement v))
            {
                if (obrigatorio) throw new PerfilInvalidoException($"{onde}: falta '{campo}'");
                return padrao;
            }
            if (v.ValueKind != JsonValueKind.Number || !v.TryGetInt32(out int valor))
                throw new PerfilInvalidoException($"{onde}: '{campo}' precisa ser um numero inteiro");
            return valor;
        }
    }
}
