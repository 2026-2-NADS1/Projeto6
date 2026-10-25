using System.Diagnostics;
using System.Text;

namespace QuizArcor;

public class Form1 : Form
{
    private const int TempoPorPergunta = 20;   // segundos por pergunta

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

    private readonly Font _fonteEnunciadoGrande = Tema.Fonte(20);
    private readonly Font _fonteEnunciadoMedia = Tema.Fonte(16);

    // ---------- telas (uma por painel) ----------
    private readonly PainelImagem pnlInicio = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly PainelImagem pnlIdent = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly PainelImagem pnlQuiz = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly PainelImagem pnlResultado = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly PainelImagem pnlHistorico = new() { Dock = DockStyle.Fill, Visible = false };

    // ---------- inicio ----------
    private BotaoJogo btnJogar = null!;
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
    private Label lblClassif = null!;
    private Label lblPontosGrande = null!;
    private Label lblEnvio = null!;
    private BotaoJogo btnReenviar = null!;
    private TextBox txtResumo = null!;

    // ---------- historico ----------
    private TextBox txtHistorico = null!;

    public Form1()
    {
        Text = "Arcor – Desafio das Marcas";
        AutoScaleMode = AutoScaleMode.None;
        ClientSize = new Size(1152, 648);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Tema.AzulEscuro;
        DoubleBuffered = true;
        KeyPreview = true;

        pnlInicio.Imagem = Img("capa.png");
        pnlIdent.Imagem = Img("fundo.png");
        pnlQuiz.Imagem = Img("fundo.png");
        pnlResultado.Imagem = Img("fundo.png");
        pnlHistorico.Imagem = Img("fundo.png");

        MontarInicio();
        MontarIdentificacao();
        MontarQuiz();
        MontarResultado();
        MontarHistorico();

        Controls.AddRange(new Control[] { pnlInicio, pnlIdent, pnlQuiz, pnlResultado, pnlHistorico });

        _timerPergunta.Tick += TimerPergunta_Tick;
        _timerFlash.Tick += TimerFlash_Tick;
        KeyDown += AoTeclar;
        FormClosed += (_, _) =>
        {
            _timerPergunta.Stop();
            _timerFlash.Stop();
        };

        MostrarPainel(pnlInicio);
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
    private static Label Rotulo(string texto, float tamanho, Color cor,
        ContentAlignment alinhamento = ContentAlignment.MiddleLeft, FontStyle estilo = FontStyle.Bold)
    {
        return new Label
        {
            Text = texto,
            Font = Tema.Fonte(tamanho, estilo),
            ForeColor = cor,
            BackColor = Color.Transparent,
            AutoSize = false,
            TextAlign = alinhamento,
            UseMnemonic = false
        };
    }

    private static BotaoJogo Btn(string texto, int x, int y, int w, int h, Color cor, Color corTexto, float fonte = 12)
    {
        return new BotaoJogo
        {
            Text = texto,
            Bounds = new Rectangle(x, y, w, h),
            Cor = cor,
            CorTexto = corTexto,
            Font = Tema.Fonte(fonte),
            Raio = Math.Min(16, (h - 8) / 2)
        };
    }

    private void MostrarPainel(Control painel)
    {
        if (painel != pnlQuiz) _timerPergunta.Stop();

        foreach (var p in new Control[] { pnlInicio, pnlIdent, pnlQuiz, pnlResultado, pnlHistorico })
            p.Visible = p == painel;
    }

    // =====================================================================
    // TELA INICIAL (a capa ja tem titulo e mascote; so ficam os botoes)
    // =====================================================================
    private void MontarInicio()
    {
        if (pnlInicio.Imagem == null)
        {
            // sem a imagem capa.png: mostra um titulo simples
            var titulo = Rotulo("ARCOR" + Environment.NewLine + "Desafio das Marcas", 40, Color.White, ContentAlignment.MiddleCenter);
            titulo.Bounds = new Rectangle(0, 150, 1152, 200);
            pnlInicio.Controls.Add(titulo);
        }

        var btnComoJogar = Btn("Como jogar", 676, 578, 120, 44, Tema.Marrom, Tema.Creme, 11);
        btnJogar = Btn("JOGAR", 806, 570, 200, 56, Tema.Azul, Color.White, 18);
        var btnHist = Btn("Histórico", 1016, 578, 120, 44, Tema.Marrom, Tema.Creme, 11);
        pnlInicio.Controls.Add(btnComoJogar);
        pnlInicio.Controls.Add(btnJogar);
        pnlInicio.Controls.Add(btnHist);

        // cartao "Como jogar" (objetivo e instrucoes)
        cardComoJogar = new PainelCartao { Bounds = new Rectangle(276, 60, 600, 470), Visible = false };

        string instrucoes = string.Join(Environment.NewLine, new[]
        {
            "Objetivo: descobrir o quanto você conhece as marcas e os produtos da Arcor, como Tortuguita e Block.",
            "",
            "1. Informe um apelido (ou jogue como anônimo) e escolha a dificuldade.",
            $"2. Você tem {TempoPorPergunta} segundos para escolher uma alternativa.",
            "3. Precisa de ajuda? Use a pista: cada pista custa 5 pontos.",
            "4. Cada acerto vale 10 pontos (fácil), 20 (médio) ou 30 (difícil).",
            "5. No final, veja o resumo, a classificação e as marcas da partida."
        });

        var tituloCard = Rotulo("Como jogar", 24, Tema.Marrom, ContentAlignment.MiddleCenter);
        tituloCard.Bounds = new Rectangle(20, 18, 560, 50);
        var textoCard = Rotulo(instrucoes, 12, Tema.Marrom, ContentAlignment.TopLeft, FontStyle.Regular);
        textoCard.Bounds = new Rectangle(34, 78, 532, 300);
        var btnEntendi = Btn("Entendi!", 190, 392, 220, 52, Tema.Verde, Color.White, 14);

        cardComoJogar.Controls.Add(tituloCard);
        cardComoJogar.Controls.Add(textoCard);
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
    }

    // =====================================================================
    // IDENTIFICACAO (cartao centralizado na janela)
    // =====================================================================
    private void MontarIdentificacao()
    {
        // cartao 900 x 520 no centro da janela de 1152 x 648
        var card = new PainelCartao { Bounds = new Rectangle(126, 64, 900, 520) };
        pnlIdent.Controls.Add(card);

        var mascote = new PictureBox
        {
            Bounds = new Rectangle(24, 50, 220, 410),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            Image = Img("mascote.png")
        };
        card.Controls.Add(mascote);

        var titulo = Rotulo("Quem vai jogar?", 26, Tema.Marrom, ContentAlignment.MiddleCenter);
        titulo.Bounds = new Rectangle(270, 28, 606, 52);
        card.Controls.Add(titulo);

        var lblApelido = Rotulo("Seu apelido:", 13, Tema.Marrom);
        lblApelido.Bounds = new Rectangle(270, 96, 606, 28);
        card.Controls.Add(lblApelido);

        txtNick = new TextBox
        {
            Bounds = new Rectangle(270, 128, 606, 36),
            Font = Tema.Fonte(16, FontStyle.Regular),
            BorderStyle = BorderStyle.FixedSingle,
            MaxLength = 30,
            BackColor = Color.White,
            ForeColor = Tema.Marrom
        };
        card.Controls.Add(txtNick);

        chkAnonimo = new CheckBox
        {
            Text = "Jogar como anônimo",
            Bounds = new Rectangle(270, 176, 606, 30),
            Font = Tema.Fonte(12, FontStyle.Regular),
            ForeColor = Tema.Marrom,
            BackColor = Color.Transparent,
            AutoSize = false
        };
        chkAnonimo.CheckedChanged += (_, _) => txtNick.Enabled = !chkAnonimo.Checked;
        card.Controls.Add(chkAnonimo);

        var lblDif = Rotulo("Dificuldade das perguntas:", 13, Tema.Marrom);
        lblDif.Bounds = new Rectangle(270, 220, 606, 28);
        card.Controls.Add(lblDif);

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
    private void Pilula(Rectangle area, out Label rotulo)
    {
        var card = new PainelCartao { Bounds = area, Raio = 16, Sombra = 4 };
        rotulo = Rotulo("", 12, Tema.Marrom, ContentAlignment.MiddleCenter);
        rotulo.Bounds = new Rectangle(8, 2, area.Width - 16, area.Height - 12);
        card.Controls.Add(rotulo);
        pnlQuiz.Controls.Add(card);
    }

    private void MontarQuiz()
    {
        // ----- faixa do topo -----
        Pilula(new Rectangle(40, 12, 330, 44), out lblProgresso);
        Pilula(new Rectangle(386, 12, 380, 44), out lblMarca);
        Pilula(new Rectangle(782, 12, 200, 44), out lblPontos);
        Pilula(new Rectangle(998, 12, 114, 44), out lblTempo);
        lblTempo.Font = Tema.Fonte(16);

        barraTempo = new BarraTempo { Bounds = new Rectangle(40, 66, 1072, 16) };
        pnlQuiz.Controls.Add(barraTempo);

        // ----- pergunta -----
        cardPergunta = new PainelCartao { Bounds = new Rectangle(40, 94, 1072, 128) };
        lblEnunciado = Rotulo("", 20, Tema.Marrom, ContentAlignment.MiddleCenter);
        lblEnunciado.Bounds = new Rectangle(24, 10, 1024, 100);
        cardPergunta.Controls.Add(lblEnunciado);
        pnlQuiz.Controls.Add(cardPergunta);

        // ----- pista -----
        btnPista = Btn("Pista (-5 pts)", 40, 232, 190, 44, Tema.Amarelo, Tema.Marrom, 11);
        btnPista.Click += btnPista_Click;
        pnlQuiz.Controls.Add(btnPista);

        pilPista = new PainelCartao { Bounds = new Rectangle(246, 232, 866, 44), Raio = 16, Sombra = 4, Visible = false };
        lblPista = Rotulo("", 11, Tema.Marrom, ContentAlignment.MiddleLeft, FontStyle.Regular);
        lblPista.Bounds = new Rectangle(14, 2, 838, 32);
        pilPista.Controls.Add(lblPista);
        pnlQuiz.Controls.Add(pilPista);

        // ----- quatro alternativas coloridas (2 x 2) -----
        Color[] cores = { Tema.Azul, Tema.Amarelo, Tema.Verde, Tema.MarromClaro };
        Color[] textos = { Color.White, Tema.Marrom, Color.White, Color.White };
        Forma[] formas = { Forma.Triangulo, Forma.Losango, Forma.Circulo, Forma.Quadrado };

        for (int i = 0; i < _alts.Length; i++)
        {
            int idx = i;
            var b = new BotaoJogo
            {
                Bounds = new Rectangle(40 + (i % 2) * 546, 284 + (i / 2) * 164, 526, 150),
                Cor = cores[i],
                CorTexto = textos[i],
                Simbolo = formas[i],
                Font = Tema.Fonte(16),
                Raio = 22,
                Visible = false
            };
            b.Click += (_, _) => AoClicarAlternativa(idx);
            _alts[i] = b;
            pnlQuiz.Controls.Add(b);
        }

        // ----- cartao de feedback (aparece depois de responder) -----
        cardFeedback = new PainelCartao { Bounds = new Rectangle(40, 232, 1072, 376), Visible = false };

        picFeedback = new PictureBox
        {
            Bounds = new Rectangle(24, 16, 230, 336),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };
        lblResultado = Rotulo("", 26, Tema.Verde);
        lblResultado.Bounds = new Rectangle(280, 18, 770, 52);
        lblCorreta = Rotulo("", 14, Tema.Marrom, ContentAlignment.TopLeft);
        lblCorreta.Bounds = new Rectangle(280, 76, 770, 56);
        lblFeedback = Rotulo("", 13, Tema.Marrom, ContentAlignment.TopLeft, FontStyle.Regular);
        lblFeedback.Bounds = new Rectangle(280, 138, 770, 150);
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
                "Confirme que a API está rodando (dotnet run na pasta QuizArcorApi)." + Environment.NewLine + Environment.NewLine +
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

    // Fundo azul padrao; marrom (chocolate) nas perguntas da Block
    private void AplicarTemaMarca(string? marca)
    {
        bool block = Tema.ContemMarca(marca, "block");
        Image? fundoBlock = block ? Img("fundo-block.png") : null;

        if (block && fundoBlock == null)
        {
            pnlQuiz.Imagem = null;
            pnlQuiz.CorFundo1 = Tema.MarromClaro;
            pnlQuiz.CorFundo2 = Tema.Marrom;
        }
        else
        {
            pnlQuiz.CorFundo1 = Tema.Azul;
            pnlQuiz.CorFundo2 = Tema.AzulEscuro;
            pnlQuiz.Imagem = fundoBlock ?? Img("fundo.png");
        }
    }

    private void MostrarPergunta()
    {
        var p = _perguntas[_indice];
        _respondida = false;
        _pistaUsada = false;

        AplicarTemaMarca(p.Marca);

        lblMarca.Text = (string.IsNullOrWhiteSpace(p.Marca) ? "" : p.Marca + "  •  ") + p.Dificuldade;

        // imagem da marca ao lado do enunciado
        Image? imagemMarca = null;
        if (Tema.ContemMarca(p.Marca, "tortuguita")) imagemMarca = Img("tortuguita.png");
        else if (Tema.ContemMarca(p.Marca, "block")) imagemMarca = Img("block.png");

        cardPergunta.ImagemLateral = imagemMarca;
        cardPergunta.LarguraImagem = imagemMarca != null ? 200 : 0;
        int x0 = imagemMarca != null ? 232 : 24;
        lblEnunciado.Bounds = new Rectangle(x0, 10, 1072 - x0 - 24, 100);
        lblEnunciado.Font = p.Enunciado.Length > 110 ? _fonteEnunciadoMedia : _fonteEnunciadoGrande;
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
            IniciarFlash(Tema.Verde);
        }
        else
        {
            lblResultado.Text = letra == null ? "Tempo esgotado!" : "Ops, não foi dessa vez!";
            lblResultado.ForeColor = Tema.Vermelho;
            lblCorreta.Text = "A resposta correta era: " + correta;
            picFeedback.Image = Img("mascote-triste.png");
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

    // Teclas 1-4 ou A-D respondem; Enter/Espaco avancam
    private void AoTeclar(object? sender, KeyEventArgs e)
    {
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
        // ----- coluna da esquerda: trofeu, classificacao e pontos -----
        picTrofeu = new PictureBox
        {
            Bounds = new Rectangle(50, 90, 260, 240),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            Image = Img("trofeu.png")
        };
        lblClassif = Rotulo("", 22, Tema.Amarelo, ContentAlignment.MiddleCenter);
        lblClassif.Bounds = new Rectangle(20, 340, 320, 70);
        lblPontosGrande = Rotulo("", 20, Color.White, ContentAlignment.MiddleCenter);
        lblPontosGrande.Bounds = new Rectangle(20, 414, 320, 44);
        lblEnvio = Rotulo("", 10, Color.White, ContentAlignment.TopCenter, FontStyle.Regular);
        lblEnvio.Bounds = new Rectangle(20, 466, 320, 56);
        btnReenviar = Btn("Tentar registrar de novo", 50, 530, 260, 44, Tema.Amarelo, Tema.Marrom, 10);
        btnReenviar.Visible = false;
        btnReenviar.Click += async (_, _) => await EnviarResultadoAsync();

        pnlResultado.Controls.Add(picTrofeu);
        pnlResultado.Controls.Add(lblClassif);
        pnlResultado.Controls.Add(lblPontosGrande);
        pnlResultado.Controls.Add(lblEnvio);
        pnlResultado.Controls.Add(btnReenviar);

        // ----- direita: cartao com o resumo -----
        var card = new PainelCartao { Bounds = new Rectangle(360, 30, 760, 500) };
        txtResumo = new TextBox
        {
            Bounds = new Rectangle(20, 20, 720, 462),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 10),
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            ForeColor = Tema.Marrom
        };
        card.Controls.Add(txtResumo);
        pnlResultado.Controls.Add(card);

        var btnNovamente = Btn("Jogar novamente", 360, 548, 230, 52, Tema.Verde, Color.White, 13);
        var btnHist = Btn("Histórico", 606, 548, 200, 52, Tema.Azul, Color.White, 13);
        var btnInicio = Btn("Início", 822, 548, 160, 52, Tema.Marrom, Tema.Creme, 13);
        pnlResultado.Controls.Add(btnNovamente);
        pnlResultado.Controls.Add(btnHist);
        pnlResultado.Controls.Add(btnInicio);

        btnNovamente.Click += (_, _) =>
        {
            txtNick.Clear();
            chkAnonimo.Checked = false;
            _difIndice = 0;
            AtualizarDificuldade();
            MostrarPainel(pnlIdent);
        };
        btnHist.Click += async (_, _) => await MostrarHistoricoAsync();
        btnInicio.Click += (_, _) => MostrarPainel(pnlInicio);
    }

    private async Task FinalizarAsync()
    {
        var dados = new DadosPartida
        {
            Nickname = _nickname,
            FaixaEtaria = 6, // nao pedimos faixa etaria: dado pessoal desnecessario
            QuestoesFaceis = _jogadas.Count(j => j.Nivel == 1),
            AcertosFaceis = _jogadas.Count(j => j.Nivel == 1 && j.Acertou),
            QuestoesMedias = _jogadas.Count(j => j.Nivel == 2),
            AcertosMedias = _jogadas.Count(j => j.Nivel == 2 && j.Acertou),
            QuestoesDificeis = _jogadas.Count(j => j.Nivel == 3),
            AcertosDificeis = _jogadas.Count(j => j.Nivel == 3 && j.Acertou),
            TempoPartida = Math.Max(1, (int)Math.Round(_tempoTotalSeg)),
            DicasUsadas = _dicas
        };

        var erros = CalculadoraPartida.Validar(dados);
        if (erros.Count > 0)
        {
            MessageBox.Show(string.Join(Environment.NewLine, erros),
                "Dados inconsistentes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            MostrarPainel(pnlInicio);
            return;
        }

        _resultado = CalculadoraPartida.Calcular(dados);
        txtResumo.Text = MontarResumo(_resultado, dados, _perguntas);
        lblClassif.Text = _resultado.Classificacao;
        lblPontosGrande.Text = _resultado.Pontuacao + " pontos";
        lblEnvio.Text = "Registrando a partida no histórico...";
        btnReenviar.Visible = false;
        MostrarPainel(pnlResultado);

        await EnviarResultadoAsync();
    }

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
    // HISTORICO (cartao centralizado)
    // =====================================================================
    private void MontarHistorico()
    {
        // cartao 900 x 500 centralizado na horizontal
        var card = new PainelCartao { Bounds = new Rectangle(126, 30, 900, 500) };

        var titulo = Rotulo("Histórico de partidas", 22, Tema.Marrom, ContentAlignment.MiddleCenter);
        titulo.Bounds = new Rectangle(24, 18, 852, 46);

        txtHistorico = new TextBox
        {
            Bounds = new Rectangle(24, 76, 852, 400),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 11),
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            ForeColor = Tema.Marrom
        };

        card.Controls.Add(titulo);
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
