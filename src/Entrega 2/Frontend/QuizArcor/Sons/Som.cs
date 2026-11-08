using System.Runtime.InteropServices;
using System.Text;

namespace QuizArcor;

// =====================================================================
// Sons do jogo. Usa o MCI do Windows (winmm.dll), entao nao precisa de pacote extra.
// Procura os arquivos na pasta "Sons" (.mp3 ou .wav), igual a pasta "Imagens".
//   musica-login  -> tela inicial e identificacao
//   musica-jogo   -> durante as perguntas
//   musica-final  -> resultado e historico (ranking)
//   acerto / erro -> efeitos depois de cada resposta
// Se um arquivo nao existir, o jogo continua normalmente, so sem aquele som.
// =====================================================================
public static class Som
{
    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern int mciSendString(string comando, StringBuilder? retorno, int tamanho, IntPtr janela);

    private const int VolumeMusica = 350;    // 0 a 1000
    private const int VolumeEfeito = 850;

    private static readonly Dictionary<string, bool> _abertos = new();
    private static readonly System.Windows.Forms.Timer _timerLoop = new() { Interval = 500 };
    private static string? _musicaAtual;
    private static bool _tocando;

    public static bool Mudo { get; private set; }

    static Som()
    {
        _timerLoop.Tick += (_, _) => RepetirSeTerminou();
    }

    private static int Mci(string comando, StringBuilder? retorno = null)
    {
        return mciSendString(comando, retorno, retorno?.Capacity ?? 0, IntPtr.Zero);
    }

    // Procura Sons\<nome>.mp3 ou .wav subindo as pastas a partir do programa
    private static string? Achar(string nome)
    {
        string[] extensoes = { ".mp3", ".wav" };
        var bases = new[] { AppContext.BaseDirectory, Environment.CurrentDirectory };

        foreach (var baseDir in bases)
        {
            var atual = new DirectoryInfo(baseDir);
            while (atual != null)
            {
                foreach (string ext in extensoes)
                {
                    string caminho = Path.Combine(atual.FullName, "Sons", nome + ext);
                    if (File.Exists(caminho)) return caminho;
                }
                atual = atual.Parent;
            }
        }

        return null;
    }

    private static bool Abrir(string alias, string nome)
    {
        if (_abertos.TryGetValue(alias, out bool jaAberto)) return jaAberto;

        bool aberto = false;
        string? caminho = Achar(nome);
        if (caminho != null)
        {
            string tipo = caminho.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ? "mpegvideo" : "waveaudio";
            aberto = Mci($"open \"{caminho}\" type {tipo} alias {alias}") == 0;
        }

        _abertos[alias] = aberto;
        return aberto;
    }

    // Toca uma musica em loop. Se ja for a que esta tocando, nao reinicia.
    public static void Musica(string? nome)
    {
        if (nome == _musicaAtual && (_tocando || Mudo)) return;

        Parar();
        _musicaAtual = nome;
        if (nome == null || Mudo) return;

        string alias = "mus_" + nome;
        if (!Abrir(alias, nome)) return;

        Mci($"setaudio {alias} volume to {VolumeMusica}");
        Mci($"play {alias} from 0");
        _tocando = true;
        _timerLoop.Start();
    }

    private static void Parar()
    {
        _timerLoop.Stop();
        if (_musicaAtual != null && _tocando) Mci($"stop mus_{_musicaAtual}");
        _tocando = false;
    }

    // O MCI nao repete sozinho: quando a musica termina, tocamos de novo
    private static void RepetirSeTerminou()
    {
        if (!_tocando || _musicaAtual == null) return;

        var estado = new StringBuilder(64);
        Mci($"status mus_{_musicaAtual} mode", estado);
        if (estado.ToString().Equals("stopped", StringComparison.OrdinalIgnoreCase))
            Mci($"play mus_{_musicaAtual} from 0");
    }

    // Efeito curto (acerto, erro). Pode tocar junto com a musica.
    public static void Efeito(string nome)
    {
        if (Mudo) return;

        string alias = "fx_" + nome;
        if (!Abrir(alias, nome)) return;

        Mci($"setaudio {alias} volume to {VolumeEfeito}");
        Mci($"play {alias} from 0");
    }

    public static void AlternarMudo()
    {
        Mudo = !Mudo;

        if (Mudo)
        {
            Parar();
        }
        else if (_musicaAtual != null)
        {
            string nome = _musicaAtual;
            _musicaAtual = null;
            Musica(nome);
        }
    }

    public static void Fechar()
    {
        _timerLoop.Stop();
        _tocando = false;
        Mci("close all");
        _abertos.Clear();
    }
}
