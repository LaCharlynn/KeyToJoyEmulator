// GravadorPerfil.cs - escreve o arquivo de perfis no MESMO formato que o CarregadorPerfil aceita.
//
// Ele existe para a interface de configuracao (projeto Configurador) nao inventar um segundo formato:
// a leitura e a escrita do arquivo de perfis moram as duas aqui no nucleo, e as duas sao testadas pela
// bateria de logica (o teste "ida e volta" carrega cada arquivo de profiles/, regrava e compara).
//
// Regra de seguranca: `SalvarComValidacao` NUNCA troca o arquivo de verdade antes de revalidar o que
// escreveu com o proprio `CarregadorPerfil`. Se o resultado nao for carregavel, o arquivo do usuario
// fica intacto e o erro sobe para a interface mostrar.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace MyVersion
{
    public static class GravadorPerfil
    {
        /// <summary>Nome de tecla a gravar para um mapeamento: o nome canonico do codigo.</summary>
        public static string NomeDeTecla(Mapeamento m)
        {
            if (m == null) return null;
            string canonico = TabelaTeclas.NomeDe(m.Tecla);
            if (canonico != null) return canonico;
            return TabelaTeclas.Canonico(m.TeclaNome);
        }

        /// <summary>Texto JSON do arquivo de perfis, no formato da secao 7 do SPEC.md.</summary>
        public static string Serializar(ArquivoPerfis arquivo)
        {
            if (arquivo == null) throw new ArgumentNullException(nameof(arquivo));

            using (MemoryStream memoria = new MemoryStream())
            {
                JsonWriterOptions opcoes = new JsonWriterOptions { Indented = true };
                using (Utf8JsonWriter w = new Utf8JsonWriter(memoria, opcoes))
                {
                    w.WriteStartObject();
                    w.WriteNumber("versao", 1);
                    w.WriteString("perfil_inicial", arquivo.PerfilInicial ?? "");
                    w.WriteNumber("tique_ms", arquivo.TiqueMs);

                    w.WriteStartObject("perfis");
                    foreach (KeyValuePair<string, Perfil> par in arquivo.Perfis)
                    {
                        Perfil p = par.Value;
                        w.WriteStartObject(par.Key);
                        w.WriteString("descricao", p.Descricao ?? "");
                        w.WriteNumber("zona_morta", p.ZonaMorta);

                        w.WriteStartArray("mapeamentos");
                        foreach (Mapeamento m in p.Mapeamentos)
                        {
                            w.WriteStartObject();
                            w.WriteString("tecla", NomeDeTecla(m) ?? "");
                            if (m.Tipo == TipoMapeamento.Botao)
                            {
                                w.WriteString("tipo", "botao");
                                w.WriteString("alvo", m.AlvoBotao.ToString());
                            }
                            else
                            {
                                w.WriteString("tipo", "eixo");
                                w.WriteString("alvo", m.AlvoEixo.ToString());
                                w.WriteNumber("sentido", m.Sentido);
                                w.WriteNumber("rampa_ms", m.RampaMs);
                                w.WriteNumber("rampa_volta_ms", m.RampaVoltaMs);
                                w.WriteNumber("valor_maximo", m.ValorMaximo);
                            }
                            w.WriteEndObject();
                        }
                        w.WriteEndArray();

                        w.WriteStartArray("atalhos");
                        foreach (Atalho a in p.Atalhos)
                        {
                            w.WriteStartObject();
                            w.WriteString("combinacao", a.Combinacao ?? "");
                            w.WriteString("perfil", a.PerfilDestino ?? "");
                            w.WriteEndObject();
                        }
                        w.WriteEndArray();

                        w.WriteEndObject();
                    }
                    w.WriteEndObject();

                    w.WriteEndObject();
                }
                return Encoding.UTF8.GetString(memoria.ToArray());
            }
        }

        /// <summary>
        /// Grava em um arquivo temporario, revalida carregando com o carregador de verdade e so entao
        /// substitui o arquivo de destino (guardando uma copia .bak da versao anterior).
        /// Devolve false e explica o motivo em `erro` se qualquer etapa falhar - nesse caso o arquivo
        /// de destino fica como estava.
        /// </summary>
        public static bool SalvarComValidacao(ArquivoPerfis arquivo, string destino, out string erro, out List<string> avisos)
        {
            erro = null;
            avisos = new List<string>();
            if (arquivo == null) { erro = "nada para salvar (arquivo de perfis vazio)"; return false; }
            if (string.IsNullOrWhiteSpace(destino)) { erro = "caminho de destino vazio"; return false; }

            string texto;
            try { texto = Serializar(arquivo); }
            catch (Exception e) { erro = "erro ao montar o JSON: " + e.Message; return false; }

            // A PROVA FINAL continua antes de tocar no arquivo do usuario: quem le de volta e o MESMO
            // carregador que o programa usa para iniciar. O rascunho usado nessa prova mora na pasta
            // temporaria do sistema, e nao ao lado do perfil.
            string rascunho = Path.Combine(Path.GetTempPath(), "KeyToJoyEmulator-prova-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                File.WriteAllText(rascunho, texto, new UTF8Encoding(false));
                try { CarregadorPerfil.Carregar(rascunho, out avisos); }
                catch (PerfilInvalidoException e)
                {
                    erro = "o que a interface ia salvar foi recusado pelo proprio carregador: " + e.Message;
                    return false;
                }
                catch (Exception e)
                {
                    erro = "erro ao revalidar o arquivo gravado: " + e.Message;
                    return false;
                }
            }
            catch (Exception e)
            {
                erro = "nao consegui escrever o rascunho de revalidacao '" + rascunho + "': " + e.Message;
                return false;
            }
            finally
            {
                try { if (File.Exists(rascunho)) File.Delete(rascunho); } catch { }
            }

            // A versao anterior continua ficando em <arquivo>.json.bak, mas ela e gravada a partir do que
            // foi LIDO antes, e nao por uma copia do proprio destino por cima dele mesmo (REA-FINDINGS.md
            // entradas 141 a 143). O que a Analise de Comportamento acusava era o "copia o alvo e
            // reescreve o alvo", repetido.
            byte[] anterior = null;
            try { if (File.Exists(destino)) anterior = File.ReadAllBytes(destino); }
            catch { anterior = null; }

            try { File.WriteAllText(destino, texto, new UTF8Encoding(false)); }
            catch (Exception e) { erro = "nao consegui gravar o arquivo de perfis: " + e.Message; return false; }

            if (anterior != null)
            {
                // o .bak e uma conveniencia: se ele falhar, o perfil novo ja esta salvo e o usuario
                // nao perde nada.
                try { File.WriteAllBytes(destino + ".bak", anterior); } catch { }
            }
            return true;
        }
    }
}
