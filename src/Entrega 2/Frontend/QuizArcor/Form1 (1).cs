using System.Diagnostics;
using System.Text;
 
namespace QuizArcor;
 
public class Form1 : Form
{
    private const int TempoPorPergunta = 20;   // segundos por pergunta
 
    // Tamanho "de projeto": todas as posicoes abaixo foram pensadas para 1152 x 648.
    // Em tela cheia (ou janela maior) tudo e ampliado na mesma proporcao.
    private const int LarguraBase = 1152;
    private const int AlturaBase = 648;
 
    // Fundos que se alternam a cada pergunta (se um arquivo faltar, o jogo pula para o proximo)
    private static readonly string[] _fundosAzuis = { "fundo.png", "fundo-azul2.jpg", "fundo-azul3.jpg", "fundo-azul4.jpg" };
    private static readonly string[] _fundosBlock = { "fundo-block.jpg", "fundo-block2.jpg" };
 
    private readonly QuizApiService _api = new();
 
    // ---------- estado da partida ----------
    private List<Pergunta> _perguntas = new();
    private int _indice;
    private string _nickname = "Anônimo";
    private int _dicas;
    private bool _respondida;
    private bool _pistaUsada;
    private double _tempoTotalSeg;
    private int _difIndice;
    private readonly List<(int Nivel, bool Acertou)> _jogadas = new();
    private readonly Stopwatch _cronoQuestao = new();
    private ResultadoPartida? _resultado;
 
    private readonly System.Windows.Forms.Timer _timerPergunta = new() { Interval = 100 };
    private readonly System.Windows.Forms.Timer _timerFlash = new() { Interval = 30 };
    private int _flashAlpha;
    private Color _flashCor = Color.Transparent;
 
    // ---------- layout proporcional (tela cheia) ----------
    private sealed record ItemLayout(Rectangle R, float Fonte, FontStyle Estilo, string Familia);
 
    private readonly Dictionary<Control, ItemLayout> _itens = new();
    private readonly Dictionary<string, Font> _fontes = new();
    private float _escala = 1f;
    private int _offX;
    private int _offY;
    private bool _layoutPronto;
    private bool _telaCheia;
 
    // ---------- telas (uma por painel) ----------
    private readonly PainelImagem pnlInicio = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly PainelImagem pnlIdent = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly PainelImagem pnlQuiz = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly PainelImagem pnlResultado = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly PainelImagem pnlHistorico = new() { Dock = DockStyle.Fill, Visible = false };
 
    // ---------- inicio ----------
    private BotaoJogo btnJogar = null!;
    private BotaoJogo btnSom = null!;
    private PainelCartao cardComoJogar = null!;
 
    // ---------- identificacao ----------
    private TextBox txtNick = null!;
    private CheckBox chkAnonimo = null!;
    private readonly BotaoJogo[] _difBotoes = new BotaoJogo[4];
    private BotaoJogo btnComecar = null!;
 
    // ---------- quiz ----------
    private Label lblProgresso = null!;
    private Label lblMarca = null!;
    private Label lblPontos = null!;
    private Label lblTempo = null!;
    private BarraTempo barraTempo = null!;
    private PainelCartao cardPergunta = null!;
    private Label lblEnunciado = null!;
    private BotaoJogo btnPista = null!;
    private PainelCartao pilPista = null!;
    private Label lblPista = null!;
    private readonly BotaoJogo[] _alts = new BotaoJogo[4];
    private PainelCartao cardFeedback = null!;
    private PictureBox picFeedback = null!;
    private Label lblResultado = null!;
    private Label lblCorreta = null!;
    private Label lblFeedback = null!;
    private BotaoJogo btnProxima = null!;
 
    // ---------- resultado ----------
    private PictureBox picTrofeu = null!;
    private Label lblTituloResultado = null!;
    private Label lblClassif = null!;
    private Label lblPontosGrande = null!;
    private Label lblPontosMax = null!;
    private Label lblEnvio = null!;
    private BotaoJogo btnReenviar = null!;
    private Label lblAcertos = null!, lblAcertosDet = null!;
    private Label lblAprov = null!, lblAprovDet = null!;
    private Label lblTempoFinal = null!, lblTempoDet = null!;
    private Label lblDicasFinal = null!, lblDicasDet = null!;
    private Label lblMelhorNivel = null!, lblMarcasPartida = null!;
    private readonly BarraTempo[] _barrasNivel = new BarraTempo[3];
    private readonly Label[] _pctNivel = new Label[3];
    private PainelCartao cardResumo = null!;
    private TextBox txtResumo = null!;
    private readonly System.Windows.Forms.Timer _timerContagem = new() { Interval = 25 };
    private int _pontosExibidos, _pontosAlvo;
 
    // ---------- historico ----------
    private TextBox txtHistorico = null!;
 
    public Form1()
    {
        Text = "Arcor – Desafio das Marcas";
        AutoScaleMode = AutoScaleMode.None;
        ClientSize = new Size(LarguraBase, AlturaBase);
        MinimumSize = new Size(640, 400);
        BackColor = Tema.AzulEscuro;
        DoubleBuffered = true;
        KeyPreview = true;
 
        pnlInicio.Imagem = Img("capa.png");
        pnlIdent.Imagem = Img("fundo-ident.jpg") ?? Img("fundo.png");
        pnlQuiz.Imagem = Img("fundo.png");
        pnlResultado.Imagem = Img("fundo-resultado.jpg") ?? Img("fundo.png");
        pnlHistorico.Imagem = Img("fundo-historico.jpg") ?? Img("fundo.png");
 
        MontarInicio();
        MontarIdentificacao();
        MontarQuiz();
        MontarResultado();
        MontarHistorico();
 
        Controls.AddRange(new Control[] { pnlInicio, pnlIdent, pnlQuiz, pnlResultado, pnlHistorico });
 
        _timerPergunta.Tick += TimerPergunta_Tick;
        _timerFlash.Tick += TimerFlash_Tick;
        _timerContagem.Tick += TimerContagem_Tick;
        KeyDown += AoTeclar;
        Resize += (_, _) => AplicarEscala();
        Shown += (_, _) => AplicarEscala();
        FormClosed += (_, _) =>
        {
            _timerPergunta.Stop();
            _timerFlash.Stop();
            _timerContagem.Stop();
            foreach (var f in _fontes.Values) f.Dispose();
            Som.Fechar();
        };
 
        _layoutPronto = true;
        EntrarTelaCheia();
        MostrarPainel(pnlInicio);
    }
 
    // =====================================================================
    // Tela cheia e escala proporcional
    // =====================================================================
    private void EntrarTelaCheia()
    {
        _telaCheia = true;
        WindowState = FormWindowState.Normal;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
 
        Screen? tela = IsHandleCreated ? Screen.FromControl(this) : Screen.PrimaryScreen;
        Bounds = tela?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
        AplicarEscala();
    }
 
    private void SairTelaCheia()
    {
        _telaCheia = false;
        FormBorderStyle = FormBorderStyle.Sizable;
        WindowState = FormWindowState.Normal;
        ClientSize = new Size(LarguraBase, AlturaBase);
        CenterToScreen();
        AplicarEscala();
    }
 
    // Registra a posicao "de projeto" (1152 x 648) de um controle; a escala e aplicada em AplicarEscala
    private T Pos<T>(T c, int x, int y, int w, int h, float fonte = 0f,
        FontStyle estilo = FontStyle.Bold, string familia = "Segoe UI") where T : Control
    {
        var item = new ItemLayout(new Rectangle(x, y, w, h), fonte, estilo, familia);
        _itens[c] = item;
        if (_layoutPronto) AplicarItem(c, item);
        return c;
    }
 
    private void AplicarEscala()
    {
        if (!_layoutPronto || ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
 
        float sx = (float)ClientSize.Width / LarguraBase;
        float sy = (float)ClientSize.Height / AlturaBase;
        _escala = Math.Min(sx, sy);
        _offX = (int)Math.Round((ClientSize.Width - LarguraBase * _escala) / 2f);
        _offY = (int)Math.Round((ClientSize.Height - AlturaBase * _escala) / 2f);
 
        SuspendLayout();
        foreach (var par in _itens) AplicarItem(par.Key, par.Value);
        ResumeLayout(false);
    }
 
    private void AplicarItem(Control c, ItemLayout item)
    {
        // controles direto sobre a tela ganham o deslocamento que centraliza o conteudo;
        // os que estao dentro de cartoes so ganham a escala
        bool naTela = c.Parent is PainelImagem;
        int ox = naTela ? _offX : 0;
        int oy = naTela ? _offY : 0;
 
        c.Bounds = new Rectangle(
            ox + (int)Math.Round(item.R.X * _escala),
            oy + (int)Math.Round(item.R.Y * _escala),
            Math.Max(1, (int)Math.Round(item.R.Width * _escala)),
            Math.Max(1, (int)Math.Round(item.R.Height * _escala)));
 
        if (item.Fonte > 0f)
            c.Font = FonteEscalada(item.Familia, item.Fonte * 96f / 72f * _escala, item.Estilo);
 
        if (c is PainelCartao cartao) cartao.Escala = _escala;
        if (c is BotaoJogo botao) botao.Escala = _escala;
    }
 
    private Font FonteEscalada(string familia, float pixels, FontStyle estilo)
    {
        pixels = (float)Math.Round(Math.Max(6f, pixels) * 2f) / 2f;
        string chave = familia + "|" + pixels.ToString("F1") + "|" + (int)estilo;
 
        if (!_fontes.TryGetValue(chave, out var fonte))
        {
            fonte = new Font(familia, pixels, estilo, GraphicsUnit.Pixel);
            _fontes[chave] = fonte;
        }
 
        return fonte;
    }
 
    // =====================================================================
    // Imagens (procura na pasta Imagens do projeto e ao lado do executavel)
    // =====================================================================
    private static readonly Dictionary<string, Image?> _imagens = new();
 
    private static Image? Img(string nome)
    {
        if (_imagens.TryGetValue(nome, out var guardada)) return guardada;
 
        Image? imagem = null;
        var bases = new[] { AppContext.BaseDirectory, Environment.CurrentDirectory };
 
        foreach (var baseDir in bases)
        {
            var atual = new DirectoryInfo(baseDir);
            while (atual != null && imagem == null)
            {
                string[] candidatos =
                {
                    Path.Combine(atual.FullName, "Imagens", nome),
                    Path.Combine(atual.FullName, nome)
                };
 
                foreach (string caminho in candidatos)
                {
                    try
                    {
                        if (File.Exists(caminho))
                        {
                            using var fs = File.OpenRead(caminho);
                            using var origem = Image.FromStream(fs);
                            imagem = new Bitmap(origem);
                            break;
                        }
                    }
                    catch
                    {
                        // se esta imagem nao abrir, tenta o proximo caminho
                    }
                }
 
                atual = atual.Parent;
            }
 
            if (imagem != null) break;
        }
 
        _imagens[nome] = imagem;
        return imagem;
    }
 
    // =====================================================================
    // Ajudantes para criar controles
    // =====================================================================
    private Label Rotulo(string texto, int x, int y, int w, int h, float tamanho, Color cor,
        ContentAlignment alinhamento = ContentAlignment.MiddleLeft, FontStyle estilo = FontStyle.Bold)
    {
        var rotulo = new Label
        {
            Text = texto,
            ForeColor = cor,
            BackColor = Color.Transparent,
            AutoSize = false,
            TextAlign = alinhamento,
            UseMnemonic = false
        };
        return Pos(rotulo, x, y, w, h, tamanho, estilo);
    }
 
    private BotaoJogo Btn(string texto, int x, int y, int w, int h, Color cor, Color corTexto, float fonte = 12)
    {
        var botao = new BotaoJogo
        {
            Text = texto,
            Cor = cor,
            CorTexto = corTexto,
            Raio = Math.Min(16, (h - 8) / 2)
        };
        return Pos(botao, x, y, w, h, fonte);
    }
 
    private void MostrarPainel(Control painel)
    {
        if (painel != pnlQuiz) _timerPergunta.Stop();
        if (painel != pnlResultado) _timerContagem.Stop();
 
        foreach (var p in new Control[] { pnlInicio, pnlIdent, pnlQuiz, pnlResultado, pnlHistorico })
            p.Visible = p == painel;
 
        // musica de cada tela
        string musica = painel == pnlInicio || painel == pnlIdent ? "musica-login"
                      : painel == pnlQuiz ? "musica-jogo"
                      : "musica-final";
        Som.Musica(musica);
    }
 
    // =====================================================================
    // TELA INICIAL (a capa ja tem titulo e mascote; so ficam os botoes)
    // =====================================================================
    private void MontarInicio()
    {
        if (pnlInicio.Imagem == null)
        {
            // sem a imagem capa.png: mostra um titulo simples
            pnlInicio.Controls.Add(Rotulo("ARCOR" + Environment.NewLine + "Desafio das Marcas",
                0, 150, LarguraBase, 200, 40, Color.White, ContentAlignment.MiddleCenter));
        }
 
        var btnComoJogar = Btn("Como jogar", 676, 578, 120, 44, Tema.Marrom, Tema.Creme, 11);
        btnJogar = Btn("JOGAR", 806, 570, 200, 56, Tema.Azul, Color.White, 18);
        var btnHist = Btn("Histórico", 1016, 578, 120, 44, Tema.Marrom, Tema.Creme, 11);
        btnSom = Btn("Som: ligado", 14, 14, 150, 36, Tema.Azul, Color.White, 10);
        var btnSair = Btn("Sair", 1038, 14, 100, 36, Tema.Vermelho, Color.White, 11);
        pnlInicio.Controls.AddRange(new Control[] { btnComoJogar, btnJogar, btnHist, btnSom, btnSair });
 
        // cartao "Como jogar" (objetivo e instrucoes)
        cardComoJogar = Pos(new PainelCartao { Visible = false }, 276, 60, 600, 470);
 
        string instrucoes = string.Join(Environment.NewLine, new[]
        {
            "Objetivo: descobrir o quanto você conhece as marcas e os produtos da Arcor, como Tortuguita e Block.",
            "",
            "1. Informe um apelido (ou jogue como anônimo) e escolha a dificuldade.",
            $"2. Você tem {TempoPorPergunta} segundos para escolher uma alternativa.",
            "3. Precisa de ajuda? Use a pista: cada pista custa 5 pontos.",
            "4. Cada acerto vale 10 pontos (fácil), 20 (médio) ou 30 (difícil).",
            "5. No final, veja o resumo, a classificação e as marcas da partida.",
            "",
            "Atalhos: teclas 1 a 4 respondem, F11 alterna tela cheia, M liga/desliga o som."
        });
 
        var btnEntendi = Btn("Entendi!", 190, 392, 220, 52, Tema.Verde, Color.White, 14);
        cardComoJogar.Controls.Add(Rotulo("Como jogar", 20, 18, 560, 50, 24, Tema.Marrom, ContentAlignment.MiddleCenter));
        cardComoJogar.Controls.Add(Rotulo(instrucoes, 34, 78, 532, 300, 12, Tema.Marrom, ContentAlignment.TopLeft, FontStyle.Regular));
        cardComoJogar.Controls.Add(btnEntendi);
        pnlInicio.Controls.Add(cardComoJogar);
 
        btnComoJogar.Click += (_, _) =>
        {
            cardComoJogar.Visible = true;
            cardComoJogar.BringToFront();
        };
        btnEntendi.Click += (_, _) => cardComoJogar.Visible = false;
 
        btnJogar.Click += (_, _) =>
        {
            cardComoJogar.Visible = false;
            txtNick.Clear();
            chkAnonimo.Checked = false;
            _difIndice = 0;
            AtualizarDificuldade();
            MostrarPainel(pnlIdent);
        };
 
        btnHist.Click += async (_, _) => await MostrarHistoricoAsync();
        btnSom.Click += (_, _) => AlternarSom();
        btnSair.Click += (_, _) => Close();
    }
 
    private void AlternarSom()
    {
        Som.AlternarMudo();
        btnSom.Text = Som.Mudo ? "Som: desligado" : "Som: ligado";
    }
 
    // =====================================================================
    // IDENTIFICACAO (cartao centralizado na janela)
    // =====================================================================
    private void MontarIdentificacao()
    {
        // cartao 900 x 520 no centro da area de 1152 x 648
        var card = Pos(new PainelCartao(), 126, 64, 900, 520);
        pnlIdent.Controls.Add(card);
 
        var mascote = Pos(new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            Image = Img("mascote.png")
        }, 24, 50, 220, 410);
        card.Controls.Add(mascote);
 
        card.Controls.Add(Rotulo("Quem vai jogar?", 270, 28, 606, 52, 26, Tema.Marrom, ContentAlignment.MiddleCenter));
        card.Controls.Add(Rotulo("Seu apelido:", 270, 96, 606, 28, 13, Tema.Marrom));
 
        txtNick = Pos(new TextBox
        {
            BorderStyle = BorderStyle.FixedSingle,
            MaxLength = 30,
            BackColor = Color.White,
            ForeColor = Tema.Marrom
        }, 270, 128, 606, 36, 16, FontStyle.Regular);
        card.Controls.Add(txtNick);
 
        chkAnonimo = Pos(new CheckBox
        {
            Text = "Jogar como anônimo",
            ForeColor = Tema.Marrom,
            BackColor = Color.Transparent,
            AutoSize = false
        }, 270, 176, 606, 30, 12, FontStyle.Regular);
        chkAnonimo.CheckedChanged += (_, _) => txtNick.Enabled = !chkAnonimo.Checked;
        card.Controls.Add(chkAnonimo);
 
        card.Controls.Add(Rotulo("Dificuldade das perguntas:", 270, 220, 606, 28, 13, Tema.Marrom));
 
        string[] nomes = { "Todas", "Fácil", "Médio", "Difícil" };
        for (int i = 0; i < _difBotoes.Length; i++)
        {
            int idx = i;
            var b = Btn(nomes[i], 270 + i * 154, 254, 144, 46, Tema.Azul, Color.White, 12);
            b.Click += (_, _) =>
            {
                _difIndice = idx;
                AtualizarDificuldade();
            };
            _difBotoes[i] = b;
            card.Controls.Add(b);
        }
        AtualizarDificuldade();
 
        btnComecar = Btn("COMEÇAR", 393, 338, 360, 62, Tema.Verde, Color.White, 18);
        btnComecar.Click += btnComecar_Click;
        card.Controls.Add(btnComecar);
 
        var btnVoltar = Btn("Voltar", 493, 422, 160, 44, Tema.Marrom, Tema.Creme, 11);
        btnVoltar.Click += (_, _) => MostrarPainel(pnlInicio);
        card.Controls.Add(btnVoltar);
    }
 
    private void AtualizarDificuldade()
    {
        for (int i = 0; i < _difBotoes.Length; i++)
        {
            bool selecionado = i == _difIndice;
            _difBotoes[i].Cor = selecionado ? Tema.Amarelo : Tema.Azul;
            _difBotoes[i].CorTexto = selecionado ? Tema.Marrom : Color.White;
        }
    }
 
    // =====================================================================
    // QUIZ
    // =====================================================================
    private Label Pilula(int x, int y, int w, int h, float fonte = 12)
    {
        var card = Pos(new PainelCartao { Raio = 16, Sombra = 4 }, x, y, w, h);
        var rotulo = Rotulo("", 8, 2, w - 16, h - 12, fonte, Tema.Marrom, ContentAlignment.MiddleCenter);
        card.Controls.Add(rotulo);
        pnlQuiz.Controls.Add(card);
        return rotulo;
    }
 
    private void MontarQuiz()
    {
        // ----- faixa do topo -----
        lblProgresso = Pilula(40, 12, 330, 44);
        lblMarca = Pilula(386, 12, 380, 44);
        lblPontos = Pilula(782, 12, 200, 44);
        lblTempo = Pilula(998, 12, 114, 44, 16);
 
        barraTempo = Pos(new BarraTempo(), 40, 66, 1072, 16);
        pnlQuiz.Controls.Add(barraTempo);
 
        // ----- pergunta -----
        cardPergunta = Pos(new PainelCartao(), 40, 94, 1072, 128);
        lblEnunciado = Rotulo("", 24, 10, 1024, 100, 20, Tema.Marrom, ContentAlignment.MiddleCenter);
        cardPergunta.Controls.Add(lblEnunciado);
        pnlQuiz.Controls.Add(cardPergunta);
 
        // ----- pista -----
        btnPista = Btn("Pista (-5 pts)", 40, 232, 190, 44, Tema.Amarelo, Tema.Marrom, 11);
        btnPista.Click += btnPista_Click;
        pnlQuiz.Controls.Add(btnPista);
 
        pilPista = Pos(new PainelCartao { Raio = 16, Sombra = 4, Visible = false }, 246, 232, 866, 44);
        lblPista = Rotulo("", 14, 2, 838, 32, 11, Tema.Marrom, ContentAlignment.MiddleLeft, FontStyle.Regular);
        pilPista.Controls.Add(lblPista);
        pnlQuiz.Controls.Add(pilPista);
 
        // ----- quatro alternativas coloridas (2 x 2); o texto diminui sozinho se for longo -----
        Color[] cores = { Tema.Azul, Tema.Amarelo, Tema.Verde, Tema.MarromClaro };
        Color[] textos = { Color.White, Tema.Marrom, Color.White, Color.White };
        Forma[] formas = { Forma.Triangulo, Forma.Losango, Forma.Circulo, Forma.Quadrado };
 
        for (int i = 0; i < _alts.Length; i++)
        {
            int idx = i;
            var b = Pos(new BotaoJogo
            {
                Cor = cores[i],
                CorTexto = textos[i],
                Simbolo = formas[i],
                Raio = 22,
                Visible = false
            }, 40 + (i % 2) * 546, 284 + (i / 2) * 164, 526, 150, 16);
            b.Click += (_, _) => AoClicarAlternativa(idx);
            _alts[i] = b;
            pnlQuiz.Controls.Add(b);
        }
 
        // ----- cartao de feedback (aparece depois de responder) -----
        cardFeedback = Pos(new PainelCartao { Visible = false }, 40, 232, 1072, 376);
 
        picFeedback = Pos(new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        }, 24, 16, 230, 336);
        lblResultado = Rotulo("", 280, 18, 770, 52, 26, Tema.Verde);
        lblCorreta = Rotulo("", 280, 76, 770, 56, 14, Tema.Marrom, ContentAlignment.TopLeft);
        lblFeedback = Rotulo("", 280, 138, 770, 150, 13, Tema.Marrom, ContentAlignment.TopLeft, FontStyle.Regular);
        btnProxima = Btn("Próxima", 790, 296, 250, 56, Tema.Azul, Color.White, 15);
        btnProxima.Click += btnProxima_Click;
 
        cardFeedback.Controls.Add(picFeedback);
        cardFeedback.Controls.Add(lblResultado);
        cardFeedback.Controls.Add(lblCorreta);
        cardFeedback.Controls.Add(lblFeedback);
        cardFeedback.Controls.Add(btnProxima);
        pnlQuiz.Controls.Add(cardFeedback);
    }
 
    private async void btnComecar_Click(object? sender, EventArgs e)
    {
        string nick = chkAnonimo.Checked ? "" : txtNick.Text.Trim();
        _nickname = nick.Length == 0 ? "Anônimo" : nick;
 
        string[] filtros = { "", "Fácil", "Médio", "Difícil" };
        string? dificuldade = _difIndice > 0 ? filtros[_difIndice] : null;
 
        btnComecar.Enabled = false;
        try
        {
            var lista = await _api.ObterPerguntasAsync(dificuldade, 10);
 
            // descarta perguntas com dados inconsistentes
            _perguntas = lista
                .Where(p => p.Alternativas.Count >= 2
                         && p.Alternativas.Any(a => a.Letra == p.RespostaCorreta))
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            MessageBox.Show(
                "Não foi possível carregar as perguntas." + Environment.NewLine + Environment.NewLine +
                "Confirme que a API está rodando (dotnet run na pasta QuizArcorApi, ou o Iniciar.bat)." + Environment.NewLine + Environment.NewLine +
                "Detalhe: " + ex.Message,
                "Falha de conexão", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        finally
        {
            btnComecar.Enabled = true;
        }
 
        if (_perguntas.Count == 0)
        {
            MessageBox.Show(
                "Não há perguntas disponíveis para essa escolha (ou os dados estão inconsistentes)." + Environment.NewLine +
                "Tente outra dificuldade.",
                "Sem perguntas", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
 
        _indice = 0;
        _dicas = 0;
        _tempoTotalSeg = 0;
        _jogadas.Clear();
        _resultado = null;
 
        MostrarPergunta();
        MostrarPainel(pnlQuiz);
    }
 
    // Procura, a partir de "indice", o primeiro fundo que existe na lista
    private Image? EscolherFundo(string[] nomes, int indice)
    {
        for (int k = 0; k < nomes.Length; k++)
        {
            var imagem = Img(nomes[(indice + k) % nomes.Length]);
            if (imagem != null) return imagem;
        }
 
        return null;
    }
 
    // Fundo azul nas perguntas em geral; marrom (chocolate) nas perguntas da Block.
    // O fundo muda a cada pergunta.
    private void AplicarTemaMarca(string? marca, int indice)
    {
        bool block = Tema.ContemMarca(marca, "block");
 
        pnlQuiz.CorFundo1 = block ? Tema.MarromClaro : Tema.Azul;
        pnlQuiz.CorFundo2 = block ? Tema.Marrom : Tema.AzulEscuro;
        pnlQuiz.Imagem = EscolherFundo(block ? _fundosBlock : _fundosAzuis, indice);
    }
 
    private void MostrarPergunta()
    {
        var p = _perguntas[_indice];
        _respondida = false;
        _pistaUsada = false;
 
        AplicarTemaMarca(p.Marca, _indice);
 
        lblMarca.Text = (string.IsNullOrWhiteSpace(p.Marca) ? "" : p.Marca + "  •  ") + p.Dificuldade;
 
        // imagem da marca ao lado do enunciado
        Image? imagemMarca = null;
        if (Tema.ContemMarca(p.Marca, "tortuguita")) imagemMarca = Img("tortuguita.png");
        else if (Tema.ContemMarca(p.Marca, "block")) imagemMarca = Img("block.png");
 
        cardPergunta.ImagemLateral = imagemMarca;
        cardPergunta.LarguraImagem = imagemMarca != null ? 200 : 0;
        int x0 = imagemMarca != null ? 232 : 24;
        Pos(lblEnunciado, x0, 10, 1072 - x0 - 24, 100, p.Enunciado.Length > 110 ? 16 : 20);
        lblEnunciado.Text = p.Enunciado;
        cardPergunta.Invalidate();
 
        lblPista.Text = "";
        pilPista.Visible = false;
        btnPista.Visible = true;
        cardFeedback.Visible = false;
 
        for (int i = 0; i < _alts.Length; i++)
        {
            var b = _alts[i];
            if (i < p.Alternativas.Count)
            {
                b.Text = p.Alternativas[i].Texto;
                b.Visible = true;
            }
            else
            {
                b.Visible = false;
            }
        }
 
        AtualizarTopo();
 
        barraTempo.Valor = 1f;
        lblTempo.Text = TempoPorPergunta + " s";
        _cronoQuestao.Restart();
        _timerPergunta.Start();
    }
 
    private int PontosAtuais()
    {
        int pontos = _jogadas.Where(j => j.Acertou).Sum(j => j.Nivel * 10) - _dicas * 5;
        return Math.Max(0, pontos);
    }
 
    private void AtualizarTopo()
    {
        lblProgresso.Text = $"Pergunta {_indice + 1} de {_perguntas.Count}";
        lblPontos.Text = $"Pontos: {PontosAtuais()}";
    }
 
    private void TimerPergunta_Tick(object? sender, EventArgs e)
    {
        if (_respondida) return;
 
        double restante = TempoPorPergunta - _cronoQuestao.Elapsed.TotalSeconds;
        if (restante <= 0)
        {
            Responder(null);   // tempo esgotado conta como erro
            return;
        }
 
        barraTempo.Valor = (float)(restante / TempoPorPergunta);
        lblTempo.Text = Math.Ceiling(restante) + " s";
    }
 
    private void AoClicarAlternativa(int indice)
    {
        if (_respondida || _indice >= _perguntas.Count) return;
 
        var p = _perguntas[_indice];
        if (indice < 0 || indice >= p.Alternativas.Count) return;
 
        Responder(p.Alternativas[indice].Letra);
    }
 
    // letra == null significa "tempo esgotado"
    private void Responder(string? letra)
    {
        if (_respondida) return;
        _respondida = true;
        _timerPergunta.Stop();
        _tempoTotalSeg += Math.Min(TempoPorPergunta, _cronoQuestao.Elapsed.TotalSeconds);
 
        var p = _perguntas[_indice];
        bool acertou = letra != null && letra == p.RespostaCorreta;
        int nivel = CalculadoraPartida.Nivel(p.Dificuldade);
        _jogadas.Add((nivel, acertou));
 
        string correta = p.Alternativas.FirstOrDefault(a => a.Letra == p.RespostaCorreta)?.Texto ?? p.RespostaCorreta;
 
        if (acertou)
        {
            lblResultado.Text = $"Resposta correta!  +{nivel * 10} pontos";
            lblResultado.ForeColor = Tema.Verde;
            lblCorreta.Text = "";
            picFeedback.Image = Img("mascote-feliz.png");
            Som.Efeito("acerto");
            IniciarFlash(Tema.Verde);
        }
        else
        {
            lblResultado.Text = letra == null ? "Tempo esgotado!" : "Ops, não foi dessa vez!";
            lblResultado.ForeColor = Tema.Vermelho;
            lblCorreta.Text = "A resposta correta era: " + correta;
            picFeedback.Image = Img("mascote-triste.png");
            Som.Efeito("erro");
            IniciarFlash(Tema.Vermelho);
        }
 
        string feedback = p.Feedback;
        if (!string.IsNullOrWhiteSpace(p.MarcaDescricao))
            feedback += Environment.NewLine + Environment.NewLine + "Sobre a marca " + p.Marca + ": " + p.MarcaDescricao;
        lblFeedback.Text = feedback;
 
        btnProxima.Text = _indice == _perguntas.Count - 1 ? "Ver resultado" : "Próxima";
        cardFeedback.Visible = true;
        cardFeedback.BringToFront();
        AtualizarTopo();
    }
 
    private void btnPista_Click(object? sender, EventArgs e)
    {
        if (_respondida) return;
 
        var p = _perguntas[_indice];
        if (string.IsNullOrWhiteSpace(p.Pista))
        {
            lblPista.Text = "Esta pergunta não tem pista.";
            pilPista.Visible = true;
            return;
        }
 
        // a pista so custa pontos na primeira vez que e aberta em cada pergunta
        if (!_pistaUsada)
        {
            _pistaUsada = true;
            _dicas++;
        }
 
        lblPista.Text = "Pista: " + p.Pista;
        pilPista.Visible = true;
        AtualizarTopo();
    }
 
    private async void btnProxima_Click(object? sender, EventArgs e)
    {
        _indice++;
        if (_indice < _perguntas.Count) MostrarPergunta();
        else await FinalizarAsync();
    }
 
    // Teclas: F11 = tela cheia, M = som; no quiz, 1-4 ou A-D respondem e Enter/Espaco avancam
    private void AoTeclar(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F11)
        {
            e.Handled = true;
            if (_telaCheia) SairTelaCheia();
            else EntrarTelaCheia();
            return;
        }
 
        if (e.KeyCode == Keys.M && ActiveControl is not TextBox)
        {
            e.Handled = true;
            AlternarSom();
            return;
        }
 
        if (!pnlQuiz.Visible) return;
 
        if (cardFeedback.Visible)
        {
            if (e.KeyCode is Keys.Enter or Keys.Space)
            {
                e.SuppressKeyPress = true;
                btnProxima_Click(btnProxima, EventArgs.Empty);
            }
            return;
        }
 
        int indice = e.KeyCode switch
        {
            Keys.D1 or Keys.NumPad1 or Keys.A => 0,
            Keys.D2 or Keys.NumPad2 or Keys.B => 1,
            Keys.D3 or Keys.NumPad3 or Keys.C => 2,
            Keys.D4 or Keys.NumPad4 or Keys.D => 3,
            _ => -1
        };
 
        if (indice >= 0) AoClicarAlternativa(indice);
    }
 
    // ----- flash colorido (verde = acertou, vermelho = errou) -----
    private void IniciarFlash(Color cor)
    {
        _flashCor = cor;
        _flashAlpha = 120;
        pnlQuiz.Flash = Color.FromArgb(_flashAlpha, cor);
        _timerFlash.Start();
    }
 
    private void TimerFlash_Tick(object? sender, EventArgs e)
    {
        _flashAlpha -= 10;
        if (_flashAlpha <= 0)
        {
            _flashAlpha = 0;
            _timerFlash.Stop();
            pnlQuiz.Flash = Color.Transparent;
            return;
        }
 
        pnlQuiz.Flash = Color.FromArgb(_flashAlpha, _flashCor);
    }
 
    // =====================================================================
    // RESULTADO FINAL
    // =====================================================================
    private void MontarResultado()
    {
        pnlResultado.Controls.Add(Pos(new PictureBox { Image=Img("logo-grupo-arcor.png"), SizeMode=PictureBoxSizeMode.Zoom, BackColor=Color.Transparent }, 24, 8, 120, 72));
        lblTituloResultado = Rotulo("Fim de jogo!", 170, 20, 812, 52, 25, Color.White, ContentAlignment.MiddleCenter);
        pnlResultado.Controls.Add(lblTituloResultado);

        var cardPontos = Pos(new PainelCartao(), 32, 100, 390, 430);
        picTrofeu = Pos(new PictureBox { SizeMode=PictureBoxSizeMode.Zoom, BackColor=Color.Transparent }, 20, 12, 350, 142);
        var sua = Rotulo("SUA PONTUAÇÃO", 20, 162, 350, 24, 12, Tema.MarromClaro, ContentAlignment.MiddleCenter);
        lblPontosGrande = Rotulo("0", 20, 185, 350, 78, 48, Tema.Azul, ContentAlignment.MiddleCenter);
        lblPontosMax = Rotulo("", 20, 264, 350, 24, 11, Tema.Marrom, ContentAlignment.MiddleCenter, FontStyle.Regular);
        var selo = Pos(new PainelCartao { Preenchimento=Tema.Amarelo, Raio=18, Sombra=3 }, 48, 296, 294, 52);
        lblClassif = Rotulo("", 8, 4, 278, 42, 16, Tema.Marrom, ContentAlignment.MiddleCenter);
        selo.Controls.Add(lblClassif);
        lblEnvio = Rotulo("", 12, 356, 366, 34, 9, Tema.Marrom, ContentAlignment.MiddleCenter, FontStyle.Regular);
        cardPontos.Controls.AddRange(new Control[]{picTrofeu,sua,lblPontosGrande,lblPontosMax,selo,lblEnvio});
        pnlResultado.Controls.Add(cardPontos);
        btnReenviar = Btn("Tentar registrar de novo", 60, 540, 330, 42, Tema.Amarelo, Tema.Marrom, 11);
        btnReenviar.Visible=false; btnReenviar.Click += async (_,_)=>await EnviarResultadoAsync(); pnlResultado.Controls.Add(btnReenviar);

        Bloco(new Rectangle(440,100,158,112),"ACERTOS",Tema.Verde,out lblAcertos,out lblAcertosDet);
        Bloco(new Rectangle(608,100,158,112),"APROVEITAMENTO",Tema.Azul,out lblAprov,out lblAprovDet);
        Bloco(new Rectangle(776,100,158,112),"TEMPO",Tema.Marrom,out lblTempoFinal,out lblTempoDet);
        Bloco(new Rectangle(944,100,158,112),"PISTAS",Tema.Vermelho,out lblDicasFinal,out lblDicasDet);
        var niveis = Pos(new PainelCartao(),440,224,662,190);
        niveis.Controls.Add(Rotulo("Desempenho por nível",20,12,300,28,13,Tema.Marrom));
        lblMelhorNivel=Rotulo("",330,12,310,28,10,Tema.MarromClaro,ContentAlignment.MiddleRight);
        niveis.Controls.Add(lblMelhorNivel);
        string[] nomes={"Fácil","Médio","Difícil"};
        for(int i=0;i<3;i++){
            int y=48+i*42;
            niveis.Controls.Add(Rotulo(nomes[i],20,y,90,28,11,Tema.Marrom));
            _barrasNivel[i]=Pos(new BarraTempo{Valor=0},112,y+5,330,18);
            _pctNivel[i]=Rotulo("",450,y,190,28,10,Tema.Marrom,ContentAlignment.MiddleLeft,FontStyle.Regular);
            niveis.Controls.Add(_barrasNivel[i]); niveis.Controls.Add(_pctNivel[i]);
        }
        pnlResultado.Controls.Add(niveis);
        var marcas=Pos(new PainelCartao{Raio=16,Sombra=4},440,424,662,82);
        lblMarcasPartida=Rotulo("",20,8,622,62,10,Tema.Marrom,ContentAlignment.MiddleLeft,FontStyle.Regular); marcas.Controls.Add(lblMarcasPartida); pnlResultado.Controls.Add(marcas);
        var btnNovamente=Btn("Jogar novamente",440,526,205,48,Tema.Verde,Color.White,12);
        var btnResumo=Btn("Resumo",652,526,140,48,Tema.Amarelo,Tema.Marrom,12);
        var btnHist=Btn("Histórico",799,526,140,48,Tema.Azul,Color.White,12);
        var btnInicio=Btn("Início",946,526,156,48,Tema.Marrom,Tema.Creme,12);
        pnlResultado.Controls.AddRange(new Control[]{btnNovamente,btnResumo,btnHist,btnInicio});
        cardResumo=Pos(new PainelCartao{Visible=false},220,42,710,510);
        txtResumo=Pos(new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.None,BackColor=Color.White,ForeColor=Tema.Marrom},20,20,670,410,10,FontStyle.Regular,"Consolas");
        var fechar=Btn("Fechar",240,442,220,48,Tema.Verde,Color.White,13); cardResumo.Controls.Add(txtResumo);cardResumo.Controls.Add(fechar);pnlResultado.Controls.Add(cardResumo);
        btnResumo.Click+=(_,_)=>{cardResumo.Visible=true;cardResumo.BringToFront();}; fechar.Click+=(_,_)=>cardResumo.Visible=false;
        btnNovamente.Click+=(_,_)=>{txtNick.Clear();chkAnonimo.Checked=false;_difIndice=0;AtualizarDificuldade();MostrarPainel(pnlIdent);};
        btnHist.Click+=async(_,_)=>await MostrarHistoricoAsync(); btnInicio.Click+=(_,_)=>MostrarPainel(pnlInicio);
    }

    private void Bloco(Rectangle area,string titulo,Color cor,out Label valor,out Label detalhe)
    {
        var card=Pos(new PainelCartao(),area.X,area.Y,area.Width,area.Height);
        card.Controls.Add(Rotulo(titulo,8,8,area.Width-16,24,9,Tema.MarromClaro,ContentAlignment.MiddleCenter));
        valor=Rotulo("—",8,34,area.Width-16,42,20,cor,ContentAlignment.MiddleCenter);
        detalhe=Rotulo("",8,76,area.Width-16,26,8,Tema.Marrom,ContentAlignment.MiddleCenter,FontStyle.Regular);
        card.Controls.Add(valor);card.Controls.Add(detalhe);pnlResultado.Controls.Add(card);
    }

    private async Task FinalizarAsync()
    {
        var dados=new DadosPartida{Nickname=_nickname,FaixaEtaria=6,
            QuestoesFaceis=_jogadas.Count(j=>j.Nivel==1),AcertosFaceis=_jogadas.Count(j=>j.Nivel==1&&j.Acertou),
            QuestoesMedias=_jogadas.Count(j=>j.Nivel==2),AcertosMedias=_jogadas.Count(j=>j.Nivel==2&&j.Acertou),
            QuestoesDificeis=_jogadas.Count(j=>j.Nivel==3),AcertosDificeis=_jogadas.Count(j=>j.Nivel==3&&j.Acertou),
            TempoPartida=Math.Max(1,(int)Math.Round(_tempoTotalSeg)),DicasUsadas=_dicas};
        var erros=CalculadoraPartida.Validar(dados);
        if(erros.Count>0){MessageBox.Show(string.Join(Environment.NewLine,erros),"Dados inconsistentes",MessageBoxButtons.OK,MessageBoxIcon.Warning);MostrarPainel(pnlInicio);return;}
        _resultado=CalculadoraPartida.Calcular(dados); PreencherResultado(_resultado,dados);
        lblEnvio.Text="Registrando a partida no histórico...";btnReenviar.Visible=false;MostrarPainel(pnlResultado);
        _pontosExibidos=0;_pontosAlvo=_resultado.Pontuacao;lblPontosGrande.Text="0";if(_pontosAlvo>0)_timerContagem.Start();
        await EnviarResultadoAsync();
    }

    private void PreencherResultado(ResultadoPartida r,DadosPartida d)
    {
        lblTituloResultado.Text=r.Nickname=="Anônimo"?"Fim de jogo!":$"Fim de jogo, {r.Nickname}!";
        picTrofeu.Image=r.PercentualGeral>=50?Img("trofeu.png"):Img("mascote.png");
        lblPontosMax.Text=$"de {r.PontuacaoMaxima} pontos possíveis";lblClassif.Text=r.Classificacao;
        lblAcertos.Text=$"{r.TotalAcertos}/{r.TotalQuestoes}";lblAcertosDet.Text=$"{r.PercentualGeral:F0}% de acerto";
        lblAprov.Text=$"{r.AproveitamentoPontuacao:F0}%";lblAprovDet.Text="dos pontos possíveis";
        lblTempoFinal.Text=r.TempoPartida>=60?$"{r.TempoPartida/60}min {r.TempoPartida%60}s":$"{r.TempoPartida}s";lblTempoDet.Text=$"Ritmo: {r.Ritmo}";
        lblDicasFinal.Text=r.DicasUsadas.ToString();lblDicasDet.Text=r.DicasUsadas==0?"nenhuma usada":$"-{r.PenalidadeDicas} pontos";
        (int ac,int q,double pct)[] ns={(d.AcertosFaceis,d.QuestoesFaceis,r.PctFacil),(d.AcertosMedias,d.QuestoesMedias,r.PctMedio),(d.AcertosDificeis,d.QuestoesDificeis,r.PctDificil)};
        for(int i=0;i<3;i++){var (ac,q,pct)=ns[i];_barrasNivel[i].Valor=q>0?(float)(pct/100):0;_pctNivel[i].Text=q>0?$"{ac}/{q} ({pct:F0}%)":"não jogado";}
        lblMelhorNivel.Text="Melhor nível: "+r.MelhorNivel;
        var ms=_perguntas.Where(p=>!string.IsNullOrWhiteSpace(p.Marca)).Select(p=>p.Marca!).Distinct().ToList();
        lblMarcasPartida.Text=ms.Count>0?"Marcas: "+string.Join(", ",ms)+Environment.NewLine+"Obrigado por jogar o Desafio das Marcas Arcor!":"Obrigado por jogar o Desafio das Marcas Arcor!";
        txtResumo.Text=MontarResumo(r,d,_perguntas);cardResumo.Visible=false;
    }

    private void TimerContagem_Tick(object? sender,EventArgs e)
    {int passo=Math.Max(1,_pontosAlvo/40);_pontosExibidos=Math.Min(_pontosAlvo,_pontosExibidos+passo);lblPontosGrande.Text=_pontosExibidos.ToString();if(_pontosExibidos>=_pontosAlvo)_timerContagem.Stop();}

    private async Task EnviarResultadoAsync()
    {
        if (_resultado == null) return;
 
        btnReenviar.Enabled = false;
        try
        {
            await _api.EnviarResultadoAsync(_resultado);
            lblEnvio.Text = "Partida registrada no histórico.";
            btnReenviar.Visible = false;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            lblEnvio.Text = "Não foi possível registrar a partida no servidor (" + ex.Message + ").";
            btnReenviar.Visible = true;
        }
        finally
        {
            btnReenviar.Enabled = true;
        }
    }
 
    private static string MontarResumo(ResultadoPartida r, DadosPartida d, List<Pergunta> perguntas)
    {
        string Linha(string nome, int acertos, int questoes, double pct) =>
            questoes > 0
                ? $"{nome} ({acertos}/{questoes})  {pct:F1}%  {new string('*', acertos)}"
                : $"{nome} não jogado";
 
        var sb = new StringBuilder();
        sb.AppendLine("===== ARCOR – DESAFIO DAS MARCAS: RESUMO DA PARTIDA =====");
        sb.AppendLine($"Jogador: {r.Nickname}");
        sb.AppendLine();
        sb.AppendLine("Desempenho por nível:");
        sb.AppendLine(Linha("Fácil  ", d.AcertosFaceis, d.QuestoesFaceis, r.PctFacil));
        sb.AppendLine(Linha("Médio  ", d.AcertosMedias, d.QuestoesMedias, r.PctMedio));
        sb.AppendLine(Linha("Difícil", d.AcertosDificeis, d.QuestoesDificeis, r.PctDificil));
        sb.AppendLine();
        sb.AppendLine($"Total: {r.TotalAcertos} acertos e {r.TotalErros} erros em {r.TotalQuestoes} questões ({r.PercentualGeral:F1}%)");
        sb.AppendLine($"Pontuação: {r.Pontuacao} de {r.PontuacaoMaxima} ({r.AproveitamentoPontuacao:F1}%)");
        sb.AppendLine($"Dicas usadas: {r.DicasUsadas} (penalidade de {r.PenalidadeDicas} pontos)");
        sb.AppendLine($"Tempo total: {r.TempoPartida / 60} min {r.TempoPartida % 60} s | Média: {r.TempoMedioPorQuestao:F1} s por questão");
        sb.AppendLine($"Ritmo: {r.Ritmo}");
        sb.AppendLine($"Melhor nível: {r.MelhorNivel}");
        sb.AppendLine($"Classificação: {r.Classificacao}");
        sb.AppendLine();
        sb.AppendLine("Marcas abordadas:");
 
        var marcas = perguntas
            .Where(p => !string.IsNullOrWhiteSpace(p.Marca))
            .GroupBy(p => p.Marca!);
 
        foreach (var grupo in marcas)
        {
            string descricao = grupo.First().MarcaDescricao ?? "";
            sb.AppendLine("- " + grupo.Key + (descricao.Length > 0 ? ": " + descricao : ""));
        }
 
        return sb.ToString();
    }
 
    // =====================================================================
    // HISTORICO / RANKING (cartao centralizado)
    // =====================================================================
    private void MontarHistorico()
    {
        // cartao 900 x 500 centralizado na horizontal
        var card = Pos(new PainelCartao(), 126, 30, 900, 500);
        card.Controls.Add(Rotulo("Histórico de partidas", 24, 18, 852, 46, 22, Tema.Marrom, ContentAlignment.MiddleCenter));
 
        txtHistorico = Pos(new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            ForeColor = Tema.Marrom
        }, 24, 76, 852, 400, 11, FontStyle.Regular, "Consolas");
        card.Controls.Add(txtHistorico);
        pnlHistorico.Controls.Add(card);
 
        var btnVoltar = Btn("Voltar ao início", 456, 548, 240, 52, Tema.Marrom, Tema.Creme, 13);
        btnVoltar.Click += (_, _) => MostrarPainel(pnlInicio);
        pnlHistorico.Controls.Add(btnVoltar);
    }
 
    private async Task MostrarHistoricoAsync()
    {
        txtHistorico.Text = "Carregando...";
        MostrarPainel(pnlHistorico);
 
        try
        {
            var lista = await _api.ObterHistoricoAsync();
            if (lista.Count == 0)
            {
                txtHistorico.Text = "Nenhuma partida registrada ainda.";
                return;
            }
 
            var sb = new StringBuilder();
            sb.AppendLine("Data/hora        Jogador          Pontos  Acertos  Tempo");
            sb.AppendLine(new string('-', 58));
 
            foreach (var h in lista)
            {
                string data = DateTime.TryParse(h.DataHora, out var dt) ? dt.ToString("dd/MM HH:mm") : (h.DataHora ?? "");
                string nome = h.Nickname.Length > 15 ? h.Nickname.Substring(0, 15) : h.Nickname;
                string acertos = $"{h.TotalAcertos}/{h.TotalQuestoes}";
                sb.AppendLine($"{data,-16} {nome,-16} {h.Pontuacao,6}  {acertos,7}  {h.TempoSegundos}s");
            }
 
            txtHistorico.Text = sb.ToString();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            txtHistorico.Text = "Não foi possível carregar o histórico: " + ex.Message;
        }
    }
}
 