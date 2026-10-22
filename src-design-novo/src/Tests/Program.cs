// Program.cs dos testes de logica (rodam SEM o driver ViGEmBus).
//
// Verificam as regras do SPEC.md que nao dependem de driver: botao (OU logico), auto-repeat,
// rampa, soma de eixos opostos, zona morta, limite, troca de perfil e validacao do arquivo.

using System;
using System.Collections.Generic;
using System.IO;
using MyVersion;

namespace Testes
{
    internal static class Program
    {
        private static int _total;
        private static int _falhas;
        private static readonly List<string> _relatorio = new List<string>();

        private static void Verificar(string nome, bool condicao, string detalhe = "")
        {
            _total++;
            if (condicao) _relatorio.Add("PASS " + nome);
            else
            {
                _falhas++;
                _relatorio.Add("FAIL " + nome + (detalhe.Length > 0 ? "  -> " + detalhe : ""));
            }
        }

        private static CodigoTecla T(string nome)
        {
            if (!TabelaTeclas.TentarResolver(nome, out CodigoTecla c)) throw new Exception("tecla de teste desconhecida: " + nome);
            return c;
        }

        private static ArquivoPerfis Arquivo(Perfil perfil, int tiqueMs = 8)
        {
            ArquivoPerfis a = new ArquivoPerfis { Versao = 1, PerfilInicial = perfil.Nome, TiqueMs = tiqueMs };
            a.Perfis[perfil.Nome] = perfil;
            return a;
        }

        private static Mapeamento MapBotao(string tecla, string alvo)
        {
            return new Mapeamento
            {
                TeclaNome = tecla,
                Tecla = T(tecla),
                Tipo = TipoMapeamento.Botao,
                AlvoBotao = (Botao)Enum.Parse(typeof(Botao), alvo, true),
            };
        }

        private static Mapeamento MapEixo(string tecla, string alvo, int sentido = 1, int rampaMs = 150, int valorMaximo = -1)
        {
            Eixo e = (Eixo)Enum.Parse(typeof(Eixo), alvo, true);
            return new Mapeamento
            {
                TeclaNome = tecla,
                Tecla = T(tecla),
                Tipo = TipoMapeamento.Eixo,
                AlvoEixo = e,
                Sentido = sentido,
                RampaMs = rampaMs,
                ValorMaximo = valorMaximo < 0 ? Faixas.ValorMaximoPadrao(e) : valorMaximo,
            };
        }

        private static void ApertarNo(Motor m, string tecla)
        {
            CodigoTecla t = T(tecla);
            m.Apertar(t.Scan, t.Estendida);
        }

        private static void SoltarNo(Motor m, string tecla)
        {
            CodigoTecla t = T(tecla);
            m.Soltar(t.Scan, t.Estendida);
        }

        private static int Main(string[] args)
        {
            Console.WriteLine("testes de logica do KeyToJoyEmulator (nao precisam do ViGEmBus)");
            Console.WriteLine();

            TesteBotaoOuLogico();
            TesteAutoRepeat();
            TesteRampa();
            TesteRampaDeVolta();
            TesteSemRampa();
            TesteSomaOposta();
            TesteZonaMorta();
            TesteZonaMortaEmGatilho();
            TesteLimite();
            TesteTrocaDePerfil();
            TesteAtalhoConsomeTecla();
            TesteValidacao();
            TesteExemploDoProjeto();
            TestePerfilDeCorrida();
            TesteCodigosDasSetas();
            TesteGravadorIdaEVolta();

            Console.WriteLine();
            foreach (string linha in _relatorio) Console.WriteLine(linha);
            Console.WriteLine();
            Console.WriteLine($"total={_total} falhas={_falhas}");
            return _falhas == 0 ? 0 : 1;
        }

        /// <summary>
        /// "Ida e volta" do gravador: carrega cada arquivo de profiles/, reescreve com o GravadorPerfil,
        /// recarrega com o MESMO carregador e compara campo por campo. E a garantia de que a interface
        /// de configuracao (que usa este gravador) nao consegue estragar um arquivo de perfis.
        /// </summary>
        private static void TesteGravadorIdaEVolta()
        {
            string pasta = Path.Combine(AppContext.BaseDirectory, "profiles");
            if (!Directory.Exists(pasta))
            {
                Verificar("gravador: existe a pasta profiles/ ao lado dos testes", false, pasta);
                return;
            }

            string[] arquivos = Directory.GetFiles(pasta, "*.json");
            Verificar("gravador: ha arquivos de perfil para testar", arquivos.Length > 0, arquivos.Length.ToString());

            foreach (string caminho in arquivos)
            {
                string nome = Path.GetFileName(caminho);

                ArquivoPerfis antes;
                try { antes = CarregadorPerfil.Carregar(caminho, out List<string> avisos0); }
                catch (PerfilInvalidoException e)
                {
                    Verificar("gravador " + nome + ": o original carrega", false, e.Message);
                    continue;
                }

                string texto = GravadorPerfil.Serializar(antes);
                string temporario = Path.Combine(Path.GetTempPath(), "gravador-ida-e-volta-" + nome);
                File.WriteAllText(temporario, texto);

                ArquivoPerfis depois;
                try
                {
                    List<string> avisos;
                    depois = CarregadorPerfil.Carregar(temporario, out avisos);
                    Verificar("gravador " + nome + ": o que eu gravei passa pelo carregador do programa", true);
                    Verificar("gravador " + nome + ": recarrega sem avisos", avisos.Count == 0, string.Join(" | ", avisos.ToArray()));
                }
                catch (PerfilInvalidoException e)
                {
                    Verificar("gravador " + nome + ": o que eu gravei passa pelo carregador do programa", false, e.Message);
                    continue;
                }

                string diferenca = CompararPerfis(antes, depois);
                Verificar("gravador " + nome + ": ida e volta sem perder nem trocar nada", diferenca == null, diferenca ?? "");
            }
        }

        /// <summary>Compara dois arquivos de perfis campo por campo. Devolve null quando sao iguais.</summary>
        private static string CompararPerfis(ArquivoPerfis a, ArquivoPerfis b)
        {
            if (a.Versao != b.Versao) return $"versao: {a.Versao} -> {b.Versao}";
            if (a.TiqueMs != b.TiqueMs) return $"tique_ms: {a.TiqueMs} -> {b.TiqueMs}";
            if (a.PerfilInicial != b.PerfilInicial) return $"perfil_inicial: '{a.PerfilInicial}' -> '{b.PerfilInicial}'";
            if (a.Perfis.Count != b.Perfis.Count) return $"quantidade de perfis: {a.Perfis.Count} -> {b.Perfis.Count}";

            List<string> chavesA = new List<string>(a.Perfis.Keys);
            List<string> chavesB = new List<string>(b.Perfis.Keys);
            for (int i = 0; i < chavesA.Count; i++)
            {
                if (chavesA[i] != chavesB[i]) return $"nome/ordem dos perfis mudou na posicao {i + 1}: '{chavesA[i]}' -> '{chavesB[i]}'";
            }

            foreach (KeyValuePair<string, Perfil> par in a.Perfis)
            {
                Perfil pa = par.Value;
                Perfil pb;
                if (!b.Perfis.TryGetValue(par.Key, out pb)) return "sumiu o perfil '" + par.Key + "'";

                if (pa.Descricao != pb.Descricao) return $"perfil '{par.Key}': descricao '{pa.Descricao}' -> '{pb.Descricao}'";
                if (pa.ZonaMorta != pb.ZonaMorta) return $"perfil '{par.Key}': zona_morta {pa.ZonaMorta} -> {pb.ZonaMorta}";
                if (pa.Mapeamentos.Count != pb.Mapeamentos.Count) return $"perfil '{par.Key}': mapeamentos {pa.Mapeamentos.Count} -> {pb.Mapeamentos.Count}";

                for (int i = 0; i < pa.Mapeamentos.Count; i++)
                {
                    Mapeamento x = pa.Mapeamentos[i];
                    Mapeamento y = pb.Mapeamentos[i];
                    string onde = $"perfil '{par.Key}', mapeamento {i + 1}";
                    if (x.Tipo != y.Tipo) return onde + ": o tipo mudou";
                    if (!x.Tecla.Equals(y.Tecla)) return onde + ": a identidade da tecla mudou (" + x.Tecla + " -> " + y.Tecla + ")";
                    if (x.Tipo == TipoMapeamento.Botao)
                    {
                        if (x.AlvoBotao != y.AlvoBotao) return onde + ": o alvo de botao mudou";
                    }
                    else
                    {
                        if (x.AlvoEixo != y.AlvoEixo) return onde + ": o alvo de eixo mudou";
                        if (x.Sentido != y.Sentido) return onde + ": sentido mudou";
                        if (x.RampaMs != y.RampaMs) return onde + ": rampa_ms mudou";
                        if (x.RampaVoltaMs != y.RampaVoltaMs) return onde + ": rampa_volta_ms mudou";
                        if (x.ValorMaximo != y.ValorMaximo) return onde + ": valor_maximo mudou";
                    }
                }

                if (pa.Atalhos.Count != pb.Atalhos.Count) return $"perfil '{par.Key}': atalhos {pa.Atalhos.Count} -> {pb.Atalhos.Count}";
                for (int i = 0; i < pa.Atalhos.Count; i++)
                {
                    Atalho x = pa.Atalhos[i];
                    Atalho y = pb.Atalhos[i];
                    if (x.Combinacao != y.Combinacao) return $"perfil '{par.Key}', atalho {i + 1}: combinacao '{x.Combinacao}' -> '{y.Combinacao}'";
                    if (x.PerfilDestino != y.PerfilDestino) return $"perfil '{par.Key}', atalho {i + 1}: destino '{x.PerfilDestino}' -> '{y.PerfilDestino}'";
                }
            }
            return null;
        }

        private static void TesteBotaoOuLogico()
        {
            Perfil p = new Perfil { Nome = "p" };
            p.Mapeamentos.Add(MapBotao("Enter", "A"));
            p.Mapeamentos.Add(MapBotao("Space", "A"));
            Motor m = new Motor(Arquivo(p));

            EstadoControle e = m.Tique(8);
            Verificar("botao: sem tecla nenhum botao apertado", e.Botoes.Count == 0);

            ApertarNo(m, "Enter");
            e = m.Tique(8);
            Verificar("botao: Enter aperta A", e.Botoes.Count == 1 && e.Botoes.Contains(Botao.A));

            ApertarNo(m, "Space");
            e = m.Tique(8);
            Verificar("botao: duas teclas do mesmo botao = um botao (OU logico)", e.Botoes.Count == 1 && e.Botoes.Contains(Botao.A));

            SoltarNo(m, "Enter");
            e = m.Tique(8);
            Verificar("botao: soltar uma das duas teclas mantem o botao", e.Botoes.Contains(Botao.A));

            SoltarNo(m, "Space");
            e = m.Tique(8);
            Verificar("botao: soltar a ultima tecla solta o botao", e.Botoes.Count == 0);
        }

        private static void TesteAutoRepeat()
        {
            Perfil p = new Perfil { Nome = "p" };
            p.Mapeamentos.Add(MapBotao("Space", "B"));
            Motor m = new Motor(Arquivo(p));

            ApertarNo(m, "Space");
            EstadoControle primeiro = m.Tique(8);
            EstadoControle depois = null;
            for (int i = 0; i < 5; i++)   // o Windows repetiria WM_KEYDOWN: 5 "keystrokes" repetidos
            {
                ApertarNo(m, "Space");
                depois = m.Tique(8);
            }
            Verificar("auto-repeat: repeticao nao muda o estado (nao pisca)",
                primeiro.Botoes.Contains(Botao.B) && depois.Botoes.Contains(Botao.B) && depois.Igual(primeiro));
            Verificar("auto-repeat: conjunto de teclas continua com 1 tecla", m.TeclasPressionadas == 1);

            SoltarNo(m, "Space");
            EstadoControle solto = m.Tique(8);
            Verificar("auto-repeat: soltar uma vez solta o botao", solto.Botoes.Count == 0);
        }

        private static void TesteRampa()
        {
            Perfil p = new Perfil { Nome = "p" };
            p.Mapeamentos.Add(MapEixo("D", "LX", 1, 150));
            Motor m = new Motor(Arquivo(p));

            ApertarNo(m, "D");
            EstadoControle e = m.Tique(50);
            double esperado = 32767.0 * 50 / 150;
            Verificar("rampa: apos 50 ms de 150 ms fica perto de 1/3 do maximo",
                Math.Abs(e.LX - esperado) <= 32767 * 0.2, $"LX={e.LX} esperado~{esperado:F0}");

            e = m.Tique(100);
            Verificar("rampa: apos 150 ms chega ao maximo", Math.Abs(e.LX - 32767) <= 32767 * 0.2, $"LX={e.LX}");

            SoltarNo(m, "D");
            e = m.Tique(75);
            Verificar("rampa: soltando, no meio do caminho o valor caiu", Math.Abs(e.LX - 32767.0 / 2) <= 32767 * 0.25, $"LX={e.LX}");

            e = m.Tique(200);
            Verificar("rampa: soltando, volta ao repouso 0", e.LX == 0, $"LX={e.LX}");
        }

        private static void TesteSomaOposta()
        {
            Perfil p = new Perfil { Nome = "p" };
            p.Mapeamentos.Add(MapEixo("D", "LX", 1, 150));
            p.Mapeamentos.Add(MapEixo("A", "LX", -1, 150));
            Motor m = new Motor(Arquivo(p));

            ApertarNo(m, "D");
            EstadoControle e = m.Tique(150);
            Verificar("soma oposta: so D leva LX ao maximo", Math.Abs(e.LX - 32767) <= 32767 * 0.2, $"LX={e.LX}");

            ApertarNo(m, "A");   // agora as duas direcoes ao mesmo tempo
            e = m.Tique(150);
            Verificar("soma oposta: duas teclas opostas somam ~0", Math.Abs(e.LX) <= 32767 * 0.05, $"LX={e.LX}");
        }

        private static void TesteZonaMorta()
        {
            Perfil p = new Perfil { Nome = "p", ZonaMorta = 2000 };
            p.Mapeamentos.Add(MapEixo("D", "LX", 1, 150));
            Motor m = new Motor(Arquivo(p));

            ApertarNo(m, "D");
            EstadoControle e = m.Tique(5);
            Verificar("zona morta: valor abaixo da zona morta sai exatamente 0", e.LX == 0, $"LX={e.LX}");

            e = m.Tique(100);
            Verificar("zona morta: acima da zona morta o valor passa", e.LX > 2000, $"LX={e.LX}");

            // REGRESSAO (defeito real, medido em 2026-10-17 no T4d): com rampa LENTA e zona morta
            // ALTA, o passo de cada tique e MENOR que a zona morta. Se o valor interno do eixo for
            // o valor JÁ filtrado pela zona morta, a rampa nunca atravessa a zona morta e o eixo
            // fica travado em 0 para sempre. A zona morta e filtro de SAIDA, nao de realimentacao.
            Perfil lento = new Perfil { Nome = "lento", ZonaMorta = 8000 };
            lento.Mapeamentos.Add(MapEixo("D", "LX", 1, 1000));
            Motor ml = new Motor(Arquivo(lento));
            ApertarNo(ml, "D");

            int zerados = 0, atravessou = 0;
            for (int i = 0; i < 30; i++)      // 30 x 8 ms = 240 ms -> ainda abaixo da zona morta
                if (ml.Tique(8).LX == 0) zerados++;
            for (int i = 0; i < 100; i++)     // mais 800 ms -> ja deve ter atravessado
                if (ml.Tique(8).LX > 8000) atravessou++;

            Verificar("zona morta: com rampa lenta a rampa ainda atravessa a zona morta (regressao)",
                      zerados == 30 && atravessou > 0,
                      $"zerados={zerados} de 30, acima da zona={atravessou} de 100");
        }

        private static void TesteLimite()
        {
            Perfil p = new Perfil { Nome = "p" };
            p.Mapeamentos.Add(MapEixo("D", "LX", 1, 150, 30000));
            p.Mapeamentos.Add(MapEixo("F", "LX", 1, 150, 30000));
            Motor m = new Motor(Arquivo(p));

            ApertarNo(m, "D");
            ApertarNo(m, "F");
            EstadoControle e = m.Tique(1000);
            Verificar("limite: soma maior que a faixa e limitada a 32767", e.LX == 32767, $"LX={e.LX}");

            Verificar("limite: gatilho satura em 255", TesteGatilhoSatura());
        }

        private static bool TesteGatilhoSatura()
        {
            Perfil p = new Perfil { Nome = "p" };
            p.Mapeamentos.Add(MapEixo("LeftShift", "RT", 1, 80, 200));
            p.Mapeamentos.Add(MapEixo("RightShift", "RT", 1, 80, 200));
            Motor m = new Motor(Arquivo(p));
            ApertarNo(m, "LeftShift");
            ApertarNo(m, "RightShift");
            return m.Tique(1000).RT == 255;
        }

        private static void TesteTrocaDePerfil()
        {
            Perfil p = new Perfil { Nome = "p" };
            p.Mapeamentos.Add(MapBotao("Enter", "A"));
            p.Atalhos.Add(new Atalho { Combinacao = "Ctrl+Alt+1", Teclas = new List<CodigoTecla> { T("Control"), T("Alt"), T("1") }, PerfilDestino = "q" });

            Perfil q = new Perfil { Nome = "q" };
            q.Mapeamentos.Add(MapBotao("Space", "B"));

            ArquivoPerfis arq = new ArquivoPerfis { Versao = 1, PerfilInicial = "p", TiqueMs = 8 };
            arq.Perfis["p"] = p;
            arq.Perfis["q"] = q;

            Motor m = new Motor(arq);
            ApertarNo(m, "Enter");
            EstadoControle e = m.Tique(8);
            Verificar("perfil: perfil inicial 'p' aperta A com Enter", e.Botoes.Contains(Botao.A) && m.PerfilAtivo == "p");

            ApertarNo(m, "Control");
            ApertarNo(m, "Alt");
            ApertarNo(m, "1");
            e = m.Tique(8);
            Verificar("perfil: atalho troca para 'q'", m.PerfilAtivo == "q");
            Verificar("perfil: no tique da troca o estado vem zerado (nao prende botao)", e.Botoes.Count == 0, e.Descrever());

            e = m.Tique(8);
            Verificar("perfil: depois da troca o botao antigo continua solto", !e.Botoes.Contains(Botao.A), e.Descrever());

            ApertarNo(m, "Space");
            e = m.Tique(8);
            Verificar("perfil: o perfil novo responde (Space aperta B)", e.Botoes.Contains(Botao.B), e.Descrever());

            SoltarNo(m, "Space");
            SoltarNo(m, "1");
            SoltarNo(m, "Alt");
            SoltarNo(m, "Control");
            SoltarNo(m, "Enter");
            e = m.Tique(8);
            Verificar("perfil: soltando tudo o estado fica vazio", e.Botoes.Count == 0, e.Descrever());
        }

        private static void TesteAtalhoConsomeTecla()
        {
            Perfil p = new Perfil { Nome = "p" };
            p.Mapeamentos.Add(MapBotao("Control", "X"));
            p.Atalhos.Add(new Atalho { Combinacao = "Ctrl+Alt+1", Teclas = new List<CodigoTecla> { T("Control"), T("Alt"), T("1") }, PerfilDestino = "q" });

            Perfil q = new Perfil { Nome = "q" };
            q.Mapeamentos.Add(MapBotao("Control", "X"));

            ArquivoPerfis arq = new ArquivoPerfis { Versao = 1, PerfilInicial = "p", TiqueMs = 8 };
            arq.Perfis["p"] = p;
            arq.Perfis["q"] = q;

            Motor m = new Motor(arq);
            ApertarNo(m, "Control");
            EstadoControle e = m.Tique(8);
            Verificar("atalho: sem o atalho completo, Control aperta X", e.Botoes.Contains(Botao.X), e.Descrever());

            ApertarNo(m, "Alt");
            ApertarNo(m, "1");
            e = m.Tique(8);
            Verificar("atalho: completar o atalho troca de perfil", m.PerfilAtivo == "q");
            Verificar("atalho: a tecla do atalho e consumida (nao alimenta o mapeamento)", e.Botoes.Count == 0, e.Descrever());
        }

        private static void TesteValidacao()
        {
            string pasta = Path.Combine(Path.GetTempPath(), "KeyToJoyEmulator-testes-perfil");
            Directory.CreateDirectory(pasta);

            Verificar("validacao: JSON quebrado e recusado",
                Recusa("json-quebrado.json", "{ \"versao\": 1, "), "deveria lancar PerfilInvalidoException");

            Verificar("validacao: alvo de botao inexistente e recusado",
                Recusa("alvo-botao.json", ValidoJson("\"alvo\": \"AA\"")), "deveria lancar");

            Verificar("validacao: alvo de eixo inexistente e recusado",
                Recusa("alvo-eixo.json", ValidoJson("\"alvo\": \"ZZ\"")), "deveria lancar");

            Verificar("validacao: sentido 0 e recusado",
                Recusa("sentido-0.json", ValidoJsonEixo(0, "\"alvo\": \"LX\"")), "deveria lancar");

            Verificar("validacao: gatilho com sentido -1 e recusado",
                Recusa("gatilho-neg.json", ValidoJsonEixo(-1, "\"alvo\": \"RT\"")), "deveria lancar");

            Verificar("validacao: tecla desconhecida e recusada",
                Recusa("tecla.json", ValidoJson("\"alvo\": \"A\"", "\"tecla\": \"TeclaQueNaoExiste\"")), "deveria lancar");

            Verificar("validacao: atalho para perfil inexistente e recusado",
                Recusa("atalho.json", "{\"versao\":1,\"perfil_inicial\":\"p\",\"perfis\":{\"p\":{\"mapeamentos\":[],\"atalhos\":[{\"combinacao\":\"Ctrl+Alt+9\",\"perfil\":\"nao-existe\"}]}}}"), "deveria lancar");

            Verificar("validacao: perfil_inicial inexistente e recusado",
                Recusa("inicial.json", "{\"versao\":1,\"perfil_inicial\":\"nao-existe\",\"perfis\":{\"p\":{\"mapeamentos\":[]}}}"), "deveria lancar");

            Verificar("validacao: zona_morta negativa e recusada",
                Recusa("zona.json", "{\"versao\":1,\"perfil_inicial\":\"p\",\"perfis\":{\"p\":{\"mapeamentos\":[],\"zona_morta\":-1}}}"), "deveria lancar");

            Verificar("validacao: valor_maximo fora da faixa e recusado",
                Recusa("maximo.json", ValidoJsonEixo(1, "\"alvo\": \"LX\", \"valor_maximo\": 40000")), "deveria lancar");

            Verificar("validacao: rampa_ms negativo e recusado",
                Recusa("rampa-neg.json", ValidoJsonEixo(1, "\"alvo\": \"LX\", \"rampa_ms\": -1")), "deveria lancar");

            Verificar("validacao: rampa_ms zero agora e valido (sem rampa, resposta no mesmo tique)",
                !Recusa("rampa-zero.json", ValidoJsonEixo(1, "\"alvo\": \"LX\", \"rampa_ms\": 0")), "deveria aceitar");

            Verificar("validacao: rampa_volta_ms negativo e recusado",
                Recusa("rampa-volta-neg.json", ValidoJsonEixo(1, "\"alvo\": \"LX\", \"rampa_volta_ms\": -2")), "deveria lancar");
        }

        private static bool Recusa(string arquivo, string conteudo)
        {
            string pasta = Path.Combine(Path.GetTempPath(), "KeyToJoyEmulator-testes-perfil");
            Directory.CreateDirectory(pasta);
            string caminho = Path.Combine(pasta, arquivo);
            File.WriteAllText(caminho, conteudo);
            try
            {
                CarregadorPerfil.Carregar(caminho, out List<string> _);
                return false;
            }
            catch (PerfilInvalidoException)
            {
                return true;
            }
        }

        private static string ValidoJson(string alvo, string tecla = "\"tecla\": \"Enter\"")
        {
            return "{\"versao\":1,\"perfil_inicial\":\"p\",\"perfis\":{\"p\":{\"mapeamentos\":[{" + tecla + ",\"tipo\":\"botao\"," + alvo + "}]}}}";
        }

        private static string ValidoJsonEixo(int sentido, string extras)
        {
            return "{\"versao\":1,\"perfil_inicial\":\"p\",\"perfis\":{\"p\":{\"mapeamentos\":[{\"tecla\":\"D\",\"tipo\":\"eixo\",\"sentido\":" + sentido + "," + extras + "}]}}}";
        }

        private static void TesteExemploDoProjeto()
        {
            string caminho = Path.Combine(AppContext.BaseDirectory, "profiles", "exemplo.json");
            if (!File.Exists(caminho))
            {
                Verificar("exemplo: profiles/exemplo.json existe ao lado dos testes", false, caminho);
                return;
            }
            try
            {
                ArquivoPerfis a = CarregadorPerfil.Carregar(caminho, out List<string> avisos);
                Verificar("exemplo: profiles/exemplo.json e valido", a.Perfis.Count == 2 && a.PerfilInicial == "navegacao", $"perfis={a.Perfis.Count}");
                Verificar("exemplo: tique_ms do exemplo e 8", a.TiqueMs == 8, a.TiqueMs.ToString());
            }
            catch (PerfilInvalidoException e)
            {
                Verificar("exemplo: profiles/exemplo.json e valido", false, e.Message);
            }
        }

        /// <summary>
        /// `rampa_ms` 0 = SEM rampa na ida: o eixo vai ao alvo no primeiro tique. Pedido do usuario em
        /// 2026-10-18: a camera seguia a mesma rampa progressiva e isso dava sensacao de atraso ao
        /// apertar o botao. (`rampa_volta_ms` 0 ja existia e continua significando volta imediata.)
        /// </summary>
        private static void TesteSemRampa()
        {
            Perfil p = new Perfil { Nome = "p" };
            Mapeamento eixo = MapEixo("J", "RX", -1, 300);
            eixo.RampaMs = 0;
            eixo.RampaVoltaMs = 0;
            p.Mapeamentos.Add(eixo);
            Motor m = new Motor(Arquivo(p));

            ApertarNo(m, "J");
            EstadoControle e1 = m.Tique(8);
            Verificar("sem rampa: rampa_ms 0 -> o eixo vai ao alvo no primeiro tique", e1.RX == -32767, e1.Descrever());
            SoltarNo(m, "J");
            EstadoControle e2 = m.Tique(8);
            Verificar("sem rampa: rampa_volta_ms 0 -> o eixo volta a zero no primeiro tique", e2.RX == 0, e2.Descrever());

            ApertarNo(m, "J");
            EstadoControle e3 = m.Tique(1);
            Verificar("sem rampa: mesmo com 1 ms o eixo ja esta no alvo (nao existe valor intermediario)",
                e3.RX == -32767, e3.Descrever());
            SoltarNo(m, "J");
            m.Tique(1);
        }

        /// <summary>
        /// Rampa de VOLTA separada (campo `rampa_volta_ms`, pedido do usuario em 2026-10-18:
        /// "retire a rampa de volta do analogico"). 0 = volta imediata; ausente = igual a de ida.
        /// </summary>
        private static void TesteRampaDeVolta()
        {
            Perfil p = new Perfil { Nome = "p" };
            Mapeamento eixo = MapEixo("D", "LX", 1, 500);
            eixo.RampaVoltaMs = 0;
            p.Mapeamentos.Add(eixo);
            Motor m = new Motor(Arquivo(p));

            ApertarNo(m, "D");
            EstadoControle e1 = m.Tique(100);
            Verificar("rampa de volta 0: a IDA continua com rampa (100 ms de 500 ms -> valor intermediario)",
                e1.LX > 0 && e1.LX < 32767, e1.Descrever());

            e1 = m.Tique(500);
            Verificar("rampa de volta 0: segurando, chega ao maximo", e1.LX == 32767, e1.Descrever());

            SoltarNo(m, "D");
            EstadoControle e2 = m.Tique(1);
            Verificar("rampa de volta 0: soltando, volta a zero no tique seguinte (imediato)", e2.LX == 0, e2.Descrever());

            // ausente = igual a de ida (nao muda o comportamento antigo)
            Perfil q = new Perfil { Nome = "q" };
            q.Mapeamentos.Add(MapEixo("D", "LX", 1, 500));
            Motor n = new Motor(Arquivo(q));
            ApertarNo(n, "D");
            n.Tique(500);
            SoltarNo(n, "D");
            EstadoControle e3 = n.Tique(1);
            Verificar("rampa de volta ausente: continua voltando pela mesma rampa da ida",
                e3.LX > 0 && e3.LX < 32767, e3.Descrever());
        }

        /// <summary>
        /// Defeito real medido em uso em 2026-10-18 (relato do usuario: "o perfil dois nao tem as teclas
        /// de aceleracao"). A zona morta era comparada em unidades do eixo; com zona_morta 3000 (escala
        /// dos sticks) o gatilho RT, cuja faixa inteira e 0..255, ficava SEMPRE em 0.
        /// Agora a zona morta e convertida para a escala do eixo.
        /// </summary>
        private static void TesteZonaMortaEmGatilho()
        {
            Perfil p = new Perfil { Nome = "p", ZonaMorta = 3000 };
            p.Mapeamentos.Add(MapEixo("W", "RT", 1, 600, 255));
            p.Mapeamentos.Add(MapEixo("D", "LX", 1, 600, 32767));
            Motor m = new Motor(Arquivo(p));

            ApertarNo(m, "W");
            EstadoControle e = m.Tique(700);
            Verificar("zona morta: gatilho com zona morta na escala do stick AINDA chega ao maximo (255)",
                e.RT == 255, e.Descrever());
            SoltarNo(m, "W");
            m.Tique(700);

            ApertarNo(m, "D");
            e = m.Tique(30);
            Verificar("zona morta: o stick continua sendo zerado perto do repouso", e.LX == 0, e.Descrever());
            e = m.Tique(700);
            Verificar("zona morta: o stick passa da zona morta e chega ao maximo", e.LX == 32767, e.Descrever());
            SoltarNo(m, "D");
            m.Tique(700);
        }

        /// <summary>
        /// Perfil de corrida (profiles/corrida.json), pedido do usuario em 2026-10-18: o port de
        /// Forza Horizon 2 aceitou o controle virtual, e faltavam os botoes B, X e o D-PAD.
        /// Este teste roda SEM controle real e SEM apertar teclas de verdade: so Motor + perfil.
        /// Alem de conferir o que foi adicionado, ele protege o que mudou de lugar (as setas
        /// deixaram de mexer na camera e a tecla L deixou de apertar o botao X).
        /// </summary>
        private static void TestePerfilDeCorrida()
        {
            string caminho = Path.Combine(AppContext.BaseDirectory, "profiles", "corrida.json");
            if (!File.Exists(caminho))
            {
                Verificar("corrida: profiles/corrida.json existe ao lado dos testes", false, caminho);
                return;
            }

            ArquivoPerfis a = CarregadorPerfil.Carregar(caminho, out List<string> _avisos);
            Verificar("corrida: o arquivo e valido e tem 4 perfis (corrida, instantanea, suave e o diagnostico dpad-ijkl)", a.Perfis.Count == 4, "perfis=" + a.Perfis.Count);
            Verificar("corrida: o perfil inicial e 'corrida-instantanea' (o escolhido pelo usuario em 2026-10-18)", a.PerfilInicial == "corrida-instantanea", a.PerfilInicial);
            Verificar("corrida: tique_ms e 8", a.TiqueMs == 8, a.TiqueMs.ToString());

            Motor m = new Motor(a);

            // 1) botoes digitais novos: B, X e o D-PAD nas setas.
            string[] teclas = { "B", "X", "Up", "Down", "Left", "Right" };
            Botao[] alvos = { Botao.B, Botao.X, Botao.UP, Botao.DOWN, Botao.LEFT, Botao.RIGHT };
            for (int i = 0; i < teclas.Length; i++)
            {
                ApertarNo(m, teclas[i]);
                EstadoControle e = m.Tique(8);
                Verificar("corrida: tecla " + teclas[i] + " aperta o botao " + alvos[i], e.Botoes.Contains(alvos[i]), e.Descrever());
                SoltarNo(m, teclas[i]);
                e = m.Tique(8);
                Verificar("corrida: soltar " + teclas[i] + " solta o botao " + alvos[i], !e.Botoes.Contains(alvos[i]), e.Descrever());
            }

            // 2) o D-PAD nao encosta nos analogicos (a seta deixou de ser camera).
            ApertarNo(m, "Up");
            ApertarNo(m, "Left");
            EstadoControle e2 = m.Tique(300);
            Verificar("corrida: setas Up+Left nao mexem nos analogicos (LX/LY/RX/RY zerados)",
                e2.LX == 0 && e2.LY == 0 && e2.RX == 0 && e2.RY == 0, e2.Descrever());
            SoltarNo(m, "Up");
            SoltarNo(m, "Left");
            m.Tique(8);

            // 3) a tecla L deixou de apertar o botao X e virou camera para a direita.
            ApertarNo(m, "L");
            EstadoControle e3 = m.Tique(400);
            Verificar("corrida: tecla L nao aperta mais o botao X", !e3.Botoes.Contains(Botao.X), e3.Descrever());
            Verificar("corrida: tecla L leva RX ao positivo (camera para a direita)", e3.RX > 0, e3.Descrever());
            SoltarNo(m, "L");
            m.Tique(400);

            // 4) camera nos quatro sentidos (IJKL).
            string[] cameraTeclas = { "J", "L" };
            for (int i = 0; i < 2; i++)
            {
                ApertarNo(m, cameraTeclas[i]);
                EstadoControle e = m.Tique(400);
                Verificar("corrida: tecla " + cameraTeclas[i] + " leva RX ao " + (i == 0 ? "negativo" : "positivo"), i == 0 ? e.RX < 0 : e.RX > 0, e.Descrever());
                SoltarNo(m, cameraTeclas[i]);
                m.Tique(400);
            }
            // 4b) a camera NAO tem mais rampa progressiva (pedido do usuario em 2026-10-18: a rampa
            //     progressiva dava sensacao de atraso ao apertar o botao). Um unico tique basta.
            ApertarNo(m, "J");
            EstadoControle eCam = m.Tique(8);
            Verificar("corrida: camera sem rampa -> J leva RX ao minimo (-32767) em um unico tique",
                eCam.RX == -32767, eCam.Descrever());
            SoltarNo(m, "J");
            eCam = m.Tique(8);
            Verificar("corrida: camera sem rampa -> soltar J zera RX em um unico tique", eCam.RX == 0, eCam.Descrever());

            ApertarNo(m, "L");
            eCam = m.Tique(8);
            Verificar("corrida: camera sem rampa -> L leva RX ao maximo (32767) em um unico tique",
                eCam.RX == 32767, eCam.Descrever());
            SoltarNo(m, "L");
            m.Tique(8);

            string[] cameraVertical = { "I", "K" };
            for (int i = 0; i < 2; i++)
            {
                ApertarNo(m, cameraVertical[i]);
                EstadoControle e = m.Tique(400);
                Verificar("corrida: tecla " + cameraVertical[i] + " leva RY ao " + (i == 0 ? "negativo" : "positivo"), i == 0 ? e.RY < 0 : e.RY > 0, e.Descrever());
                SoltarNo(m, cameraVertical[i]);
                m.Tique(400);
            }

            // 5) o que ja funcionava continua funcionando: esterco no analogico esquerdo e
            //    acelerar/frear nos gatilhos.
            ApertarNo(m, "A");
            EstadoControle e4 = m.Tique(400);
            Verificar("corrida: tecla A leva LX ao negativo (estercar a esquerda)", e4.LX < 0, e4.Descrever());
            SoltarNo(m, "A");
            m.Tique(400);

            ApertarNo(m, "D");
            EstadoControle e5 = m.Tique(400);
            Verificar("corrida: tecla D leva LX ao positivo (estercar a direita)", e5.LX > 0, e5.Descrever());
            SoltarNo(m, "D");
            m.Tique(400);

            ApertarNo(m, "W");
            EstadoControle e6 = m.Tique(400);
            Verificar("corrida: tecla W leva RT ao maximo (acelerar)", e6.RT == 255, e6.Descrever());
            SoltarNo(m, "W");
            m.Tique(400);

            ApertarNo(m, "S");
            EstadoControle e7 = m.Tique(400);
            Verificar("corrida: tecla S leva LT ao maximo (frear)", e7.LT == 255, e7.Descrever());
            SoltarNo(m, "S");
            m.Tique(400);

            // 6) as tres rampas de esterco, para o usuario escolher dentro do jogo com Ctrl+Alt+1/2/3.
            //    Ele relatou sensacao de demora com a rampa antiga de 250 ms; o padrao agora e 120 ms.
            m.TrocarPerfil("corrida");
            ApertarNo(m, "D");
            EstadoControle e8 = m.Tique(150);
            Verificar("corrida: rampa de 120 ms -> com 150 ms de tecla a estercada ja esta no maximo", e8.LX == 32767, e8.Descrever());
            SoltarNo(m, "D");
            e8 = m.Tique(300);
            Verificar("corrida: soltando D o esterco volta a zero", e8.LX == 0, e8.Descrever());

            m.TrocarPerfil("corrida-instantanea");
            ApertarNo(m, "D");
            EstadoControle e9 = m.Tique(60);
            Verificar("corrida-instantanea: rampa de 40 ms -> com 60 ms a estercada ja esta no maximo", e9.LX == 32767, e9.Descrever());
            SoltarNo(m, "D");
            m.Tique(200);

            m.TrocarPerfil("corrida-suave");
            ApertarNo(m, "D");
            EstadoControle e10 = m.Tique(150);
            Verificar("corrida-suave: rampa de 600 ms -> com 150 ms a estercada esta bem abaixo do maximo", e10.LX > 0 && e10.LX < 20000, e10.Descrever());
            SoltarNo(m, "D");
            m.Tique(900);

            m.TrocarPerfil("corrida-instantanea");
            Verificar("corrida: o perfil padrao (escolhido pelo usuario) e 'corrida-instantanea'",
                m.PerfilAtivo == "corrida-instantanea", m.PerfilAtivo);

            // 7) o defeito que o usuario viu no perfil 2 (zona morta 3000 matava os gatilhos) e a rampa
            //    de volta imediata do esterco (rampa_volta_ms 0) que ele pediu.
            m.TrocarPerfil("corrida-suave");
            ApertarNo(m, "W");
            EstadoControle e11 = m.Tique(700);
            Verificar("corrida-suave: mesmo com zona morta 3000, W acelera ate o maximo", e11.RT == 255, e11.Descrever());
            SoltarNo(m, "W");
            m.Tique(700);

            m.TrocarPerfil("corrida");
            ApertarNo(m, "D");
            m.Tique(200);
            SoltarNo(m, "D");
            EstadoControle e12 = m.Tique(1);
            Verificar("corrida: soltar D zera o esterco imediatamente (rampa de volta 0)", e12.LX == 0, e12.Descrever());

            // 8) perfil de DIAGNOSTICO "dpad-ijkl" (pedido do usuario em 2026-10-18): para testar se a
            //    falha do D-PAD no jogo tem a ver com AS TECLAS, o D-PAD sai das SETAS e vai para IJKL,
            //    e a camera vai para as SETAS. O jogo so ve bits do pad, mas o teste elimina a duvida.
            m.TrocarPerfil("dpad-ijkl");
            Verificar("dpad-ijkl: o perfil de diagnostico existe e pode ser ativado", m.PerfilAtivo == "dpad-ijkl", m.PerfilAtivo);

            string[] dpadTeclas = { "I", "J", "K", "L" };
            Botao[] dpadAlvos = { Botao.UP, Botao.LEFT, Botao.DOWN, Botao.RIGHT };
            for (int i = 0; i < dpadTeclas.Length; i++)
            {
                ApertarNo(m, dpadTeclas[i]);
                EstadoControle ed = m.Tique(8);
                Verificar("dpad-ijkl: tecla " + dpadTeclas[i] + " aperta " + dpadAlvos[i] + " e nao encosta nos analogicos",
                    ed.Botoes.Contains(dpadAlvos[i]) && ed.LX == 0 && ed.LY == 0 && ed.RX == 0 && ed.RY == 0, ed.Descrever());
                SoltarNo(m, dpadTeclas[i]);
                m.Tique(8);
            }

            ApertarNo(m, "Left");
            EstadoControle ed2 = m.Tique(8);
            Verificar("dpad-ijkl: as SETAS agora mexem na camera (Left leva RX ao negativo) e nao no D-PAD",
                ed2.RX == -32767 && !ed2.Botoes.Contains(Botao.LEFT), ed2.Descrever());
            SoltarNo(m, "Left");
            m.Tique(8);

            ApertarNo(m, "Up");
            ed2 = m.Tique(8);
            Verificar("dpad-ijkl: seta Up leva RY ao negativo (camera para cima) e nao no D-PAD",
                ed2.RY == -32767 && !ed2.Botoes.Contains(Botao.UP), ed2.Descrever());
            SoltarNo(m, "Up");
            m.Tique(8);

            m.TrocarPerfil("corrida-instantanea");
            Verificar("dpad-ijkl: depois do diagnostico da para voltar ao perfil padrao",
                m.PerfilAtivo == "corrida-instantanea", m.PerfilAtivo);
        }

        private static void TesteCodigosDasSetas()
        {
            // As quatro setas sao teclas ESTENDIDAS: o Windows entrega scan code 0x48/0x50/0x4B/0x4D
            // COM a flag LLKHF_EXTENDED. O nosso hook monta a identidade da tecla assim mesmo
            // (`new CodigoTecla((int)dados.scanCode, (dados.flags & LLKHF_EXTENDED) != 0)`) e o
            // mapeamento vindo do arquivo de perfil passa pelo MESMO resolvedor. Logo, se o resolvedor
            // devolver uma identidade DIFERENTE da que o teclado fisico produz, a seta nunca casa com o
            // mapeamento e o botao/eixo nunca acende - foi o que o uso real sugeriu em 2026-10-18
            // (D-PAD funcionou nas teclas I/J/K/L, mas nao nas setas). Este teste MEDE o que o
            // resolvedor devolve NESTA maquina.
            string[] nomes = { "Up", "Down", "Left", "Right" };
            int[] scans = { 0x48, 0x50, 0x4B, 0x4D };
            for (int i = 0; i < nomes.Length; i++)
            {
                bool achou = TabelaTeclas.TentarResolver(nomes[i], out CodigoTecla t);
                bool ok = achou && t.Scan == scans[i] && t.Estendida;
                Verificar("setas: " + nomes[i] + " resolve para scan 0x" + scans[i].ToString("X2") +
                    " ESTENDIDA (que e o que o teclado fisico entrega)", ok, achou ? t.ToString() : "nao resolveu");
            }

            // Controle do teste: uma tecla comum NAO pode sair como estendida, senao o teste nao
            // distingue os dois casos.
            bool achouI = TabelaTeclas.TentarResolver("I", out CodigoTecla ti);
            Verificar("setas: controle - a tecla I sai como NAO estendida", achouI && !ti.Estendida,
                achouI ? ti.ToString() : "nao resolveu");

            // E o caminho inverso: um codigo (scan, estendida) de seta tem que voltar como nome de seta.
            foreach (string n in nomes)
            {
                TabelaTeclas.TentarResolver(n, out CodigoTecla c);
                string volta = TabelaTeclas.NomeDe(c);
                Verificar("setas: o codigo da seta " + n + " volta como o nome " + n + " (compara identidades)",
                    volta == n, "voltou: " + (volta ?? "null"));
            }
        }
    }
}
