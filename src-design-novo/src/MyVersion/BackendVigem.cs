// BackendVigem.cs - o UNICO arquivo que fala com o ViGEmBus (SPEC.md, secao 3 e risco R1).
//
// API do wrapper, confirmada por reflexao sobre o pacote NuGet Nefarius.ViGEm.Client 1.21.256:
//   Nefarius.ViGEm.Client.ViGEmClient()                  -> cliente do barramento
//   .CreateXbox360Controller()                           -> IXbox360Controller
//   IXbox360Controller.Connect()/.Disconnect()/.SubmitReport()
//   IXbox360Controller.SetButtonState(Xbox360Button, bool)
//   IXbox360Controller.SetAxisValue(Xbox360Axis, short)
//   IXbox360Controller.SetSliderValue(Xbox360Slider, byte)
//   Xbox360Button: A B X Y Back Start LeftShoulder RightShoulder LeftThumb RightThumb Up Down Left Right
//   Xbox360Axis: LeftThumbX LeftThumbY RightThumbX RightThumbY
//   Xbox360Slider: LeftTrigger RightTrigger

using System;
using System.Collections.Generic;
using System.Threading;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace MyVersion
{
    public sealed class BackendVigem : IControleVirtual
    {
        private static readonly Dictionary<Botao, Xbox360Button> MapaBotoes = new Dictionary<Botao, Xbox360Button>
        {
            { Botao.A, Xbox360Button.A },
            { Botao.B, Xbox360Button.B },
            { Botao.X, Xbox360Button.X },
            { Botao.Y, Xbox360Button.Y },
            { Botao.LB, Xbox360Button.LeftShoulder },
            { Botao.RB, Xbox360Button.RightShoulder },
            { Botao.BACK, Xbox360Button.Back },
            { Botao.START, Xbox360Button.Start },
            { Botao.LS, Xbox360Button.LeftThumb },
            { Botao.RS, Xbox360Button.RightThumb },
            { Botao.UP, Xbox360Button.Up },
            { Botao.DOWN, Xbox360Button.Down },
            { Botao.LEFT, Xbox360Button.Left },
            { Botao.RIGHT, Xbox360Button.Right },
        };

        private static readonly Dictionary<Eixo, Xbox360Axis> MapaEixos = new Dictionary<Eixo, Xbox360Axis>
        {
            { Eixo.LX, Xbox360Axis.LeftThumbX },
            { Eixo.LY, Xbox360Axis.LeftThumbY },
            { Eixo.RX, Xbox360Axis.RightThumbX },
            { Eixo.RY, Xbox360Axis.RightThumbY },
        };

        private static readonly Dictionary<Eixo, Xbox360Slider> MapaGatilhos = new Dictionary<Eixo, Xbox360Slider>
        {
            { Eixo.LT, Xbox360Slider.LeftTrigger },
            { Eixo.RT, Xbox360Slider.RightTrigger },
        };

        private readonly Action<string> _log;
        private ViGEmClient _cliente;
        private IXbox360Controller _controle;

        public BackendVigem(Action<string> log)
        {
            _log = log ?? (_ => { });
        }

        public string Nome => "ViGEmBus (Xbox 360)";

        public void Conectar()
        {
            _cliente = new ViGEmClient();
            _controle = _cliente.CreateXbox360Controller();
            _controle.FeedbackReceived += (s, e) => _log("feedback de vibracao recebido do jogo (ignorado neste escopo)");
            _controle.Connect();

            // EMPURRAO INICIAL (defeito real, medido em 2026-10-17 - ver REA-TASK.md):
            // enquanto o estado que enviamos nao MUDA, o que o XInputGetState reporta tambem nao
            // muda. Os eixos ficam nos valores iniciais indefinidos do driver
            // (medido: LX=-3356 LY=-1869 RX=-3255 RY=-848, pacote parado em 1) ate a PRIMEIRA
            // mudanca de estado. Para o jogo nao ler um eixo torto no comeco, mandamos 1 unidade
            // no eixo esquerdo e voltamos ao neutro: 1 em 32767 e desprezivel e so dura o instante
            // entre os dois envios, mas forca o driver a publicar o estado zerado de verdade.
            Thread.Sleep(150);        // deixa o driver registrar o dispositivo antes de publicar
            _controle.SetAxisValue(Xbox360Axis.LeftThumbX, 1);
            _controle.SubmitReport();
            _controle.SetAxisValue(Xbox360Axis.LeftThumbX, 0);
            _controle.SubmitReport();
        }

        public void Aplicar(EstadoControle estado)
        {
            if (_controle == null) return;

            foreach (KeyValuePair<Botao, Xbox360Button> par in MapaBotoes)
                _controle.SetButtonState(par.Value, estado.Botoes.Contains(par.Key));

            foreach (KeyValuePair<Eixo, Xbox360Axis> par in MapaEixos)
            {
                int v = estado.Valor(par.Key);
                if (v < -32768) v = -32768;
                if (v > 32767) v = 32767;
                _controle.SetAxisValue(par.Value, (short)v);
            }

            foreach (KeyValuePair<Eixo, Xbox360Slider> par in MapaGatilhos)
            {
                int v = estado.Valor(par.Key);
                if (v < 0) v = 0;
                if (v > 255) v = 255;
                _controle.SetSliderValue(par.Value, (byte)v);
            }

            _controle.SubmitReport();
        }

        public void Desconectar()
        {
            if (_controle != null)
            {
                try { _controle.Disconnect(); }
                catch (Exception e) { _log("aviso: falha ao desconectar o controle virtual: " + e.Message); }
                _controle = null;
            }
            if (_cliente != null)
            {
                try { _cliente.Dispose(); }
                catch { }
                _cliente = null;
            }
        }

        public void Dispose() => Desconectar();
    }
}
