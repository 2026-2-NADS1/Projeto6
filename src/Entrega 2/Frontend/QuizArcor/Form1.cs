using System.Diagnostics;
using System.Text;
using System.IO;

namespace QuizArcor;

public class Form1 : Form
{
    private readonly QuizApiService _api = new();

    // ---------- estado da partida ----------
    private List<Pergunta> _perguntas = new();
    private int _indice;
    private string _nickname = "Anônimo";
    private int _dicas;
    private bool _respondida;
    private bool _pistaUsada;
    private readonly List<(int Nivel, bool Acertou)> _jogadas = new();
    private readonly Stopwatch _cronometro = new();
    private ResultadoPartida? _resultado;

    // ---------- paineis (uma "tela" cada) ----------
    private readonly FlowLayoutPanel pnlInicio = NovoPainel();
    private readonly FlowLayoutPanel pnlIdent = NovoPainel();
    private readonly FlowLayoutPanel pnlQuiz = NovoPainel();
    private readonly FlowLayoutPanel pnlResultado = NovoPainel();
    private readonly FlowLayoutPanel pnlHistorico = NovoPainel();

    // ---------- tela inicial ----------
    private readonly Button btnIniciar = Botao("Iniciar partida");
    private readonly Button btnHistInicio = Botao("Ver histórico");

    // ---------- identificacao ----------
    private readonly TextBox txtNick = new() { Width = 320, MaxLength = 30, Font = new Font("Segoe UI", 11) };
    private readonly CheckBox chkAnonimo = new()
    {
        Text = "Jogar como anônimo",
        AutoSize = true,
        Font = new Font("Segoe UI", 10),
        Margin = new Padding(0, 0, 0, 20)
    };
    private readonly ComboBox cboDif = new()
    {
        Width = 320,
        DropDownStyle = ComboBoxStyle.DropDownList,
        Font = new Font("Segoe UI", 11),
        Margin = new Padding(0, 0, 0, 20)
    };
    private readonly Button btnComecar = Botao("Começar");
    private readonly Button btnVoltarIdent = Botao("Voltar");

    // ---------- quiz ----------
    private readonly Label lblProgresso = Rotulo("", 10, true);
    private readonly Label lblMarca = Rotulo("", 10);
    private readonly Label lblEnunciado = Rotulo("", 14, true);
    private readonly Button[] _alts = new Button[4];
    private readonly Button btnPista = Botao("Mostrar pista");
    private readonly Label lblPista = Rotulo("", 10);
    private readonly Label lblResposta = Rotulo("", 12, true);
    private readonly Label lblFeedback = Rotulo("", 11);
    private readonly Button btnProxima = Botao("Próxima pergunta");

    // ---------- resultado ----------
    private readonly TextBox txtResumo = new()
    {
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Font = new Font("Consolas", 10),
        Width = 680,
        Height = 380,
        Margin = new Padding(0, 0, 0, 10)
    };
    private readonly Label lblEnvio = Rotulo("", 10);
    private readonly Button btnReenviar = Botao("Tentar registrar de novo");
    private readonly Button btnJogarNovamente = Botao("Jogar novamente");
    private readonly Button btnHistResultado = Botao("Ver histórico");

    // ---------- historico ----------
    private readonly TextBox txtHistorico = new()
    {
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Font = new Font("Consolas", 10),
        Width = 680,
        Height = 440,
        Margin = new Padding(0, 0, 0, 10)
    };
    private readonly Button btnVoltarHist = Botao("Voltar ao início");

    public Form1()
    {
        Text = "Arcor – Desafio das Marcas";
        ClientSize = new Size(1152, 648);
        MinimumSize = new Size(900, 560);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(18, 56, 112);
        BackgroundImage = CarregarImagem("fundo.png");
        BackgroundImageLayout = ImageLayout.Stretch;

        MontarInicio();
        MontarIdentificacao();
        MontarQuiz();
        MontarResultado();
        MontarHistorico();

        Controls.AddRange(new Control[] { pnlInicio, pnlIdent, pnlQuiz, pnlResultado, pnlHistorico });
        AplicarTemaVisual();
        MostrarPainel(pnlInicio);
    }

    // =====================================================================
    // Ajudantes para criar controles
    // =====================================================================
    private static FlowLayoutPanel NovoPainel() => new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        Padding = new Padding(42, 24, 42, 24),
        BackColor = Color.Transparent,
        Visible = false
    };

    private static Label Rotulo(string texto, float tamanho = 11, bool negrito = false) => new()
    {
        Text = texto,
        AutoSize = true,
        MaximumSize = new Size(680, 0),
        Font = new Font("Segoe UI", tamanho, negrito ? FontStyle.Bold : FontStyle.Regular),
        ForeColor = Color.White,
        BackColor = Color.Transparent,
        Margin = new Padding(0, 0, 0, 12)
    };

    private static Button Botao(string texto) => new()
    {
        Text = texto,
        Width = 240,
        Height = 40,
        Font = new Font("Segoe UI", 11, FontStyle.Bold),
        BackColor = Color.FromArgb(255, 196, 0),
        ForeColor = Color.FromArgb(18, 56, 112),
        FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand,
        Margin = new Padding(0, 0, 0, 10)
    };

    private void AplicarTemaVisual()
    {
        foreach (Control painel in new Control[] { pnlInicio, pnlIdent, pnlQuiz, pnlResultado, pnlHistorico })
        {
            painel.BackColor = Color.Transparent;
            AplicarTemaControles(painel);
        }

        foreach (var b in _alts)
        {
            if (b == null) continue;
            b.BackColor = Color.FromArgb(255, 244, 214);
            b.ForeColor = Color.FromArgb(74, 44, 26);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = Color.FromArgb(255, 196, 0);
            b.FlatAppearance.BorderSize = 2;
            b.Cursor = Cursors.Hand;
        }
    }

    private static void AplicarTemaControles(Control pai)
    {
        foreach (Control c in pai.Controls)
        {
            if (c is Button b)
            {
                b.BackColor = Color.FromArgb(255, 196, 0);
                b.ForeColor = Color.FromArgb(18, 56, 112);
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderColor = Color.FromArgb(255, 220, 90);
                b.FlatAppearance.BorderSize = 1;
                b.Font = new Font("Segoe UI", 11, FontStyle.Bold);
                b.Cursor = Cursors.Hand;
                b.UseVisualStyleBackColor = false;
            }
            else if (c is TextBox tb)
            {
                tb.BackColor = Color.FromArgb(255, 250, 235);
                tb.ForeColor = Color.FromArgb(18, 56, 112);
                tb.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (c is ComboBox cb)
            {
                cb.BackColor = Color.FromArgb(255, 250, 235);
                cb.ForeColor = Color.FromArgb(18, 56, 112);
            }
            else if (c is CheckBox chk)
            {
                chk.ForeColor = Color.White;
                chk.BackColor = Color.Transparent;
            }
            else if (c is Label lbl)
            {
                lbl.ForeColor = Color.White;
                lbl.BackColor = Color.Transparent;
            }

            if (c.HasChildren) AplicarTemaControles(c);
        }
    }

    // Procura as imagens na pasta imagens/Imagens do projeto e também ao lado do executável.
    private static Image? CarregarImagem(string nome)
    {
        var bases = new[] { AppContext.BaseDirectory, Environment.CurrentDirectory };
        foreach (var baseDir in bases)
        {
            var atual = new DirectoryInfo(baseDir);
            while (atual != null)
            {
                string[] candidatos =
                {
                    Path.Combine(atual.FullName, "imagens", nome),
                    Path.Combine(atual.FullName, "Imagens", nome),
                    Path.Combine(atual.FullName, nome)
                };

                foreach (string caminho in candidatos)
                {
                    try
                    {
                        if (File.Exists(caminho))
                        {
                            using var origem = Image.FromFile(caminho);
                            return new Bitmap(origem);
                        }
                    }
                    catch
                    {
                        // Se uma imagem não puder ser carregada, tenta os outros caminhos.
                    }
                }
                atual = atual.Parent;
            }
        }
        return null;
    }

    private static PictureBox? CriarImagem(string nome, int largura, int altura)
    {
        var imagem = CarregarImagem(nome);
        if (imagem == null) return null;
        return new PictureBox
        {
            Image = imagem,
            Width = largura,
            Height = altura,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 8)
        };
    }

    private void MostrarPainel(Control painel)
    {
        foreach (var p in new Control[] { pnlInicio, pnlIdent, pnlQuiz, pnlResultado, pnlHistorico })
            p.Visible = p == painel;
    }

    // =====================================================================
    // Montagem das telas
    // =====================================================================
    private void MontarInicio()
    {
        string instrucoes = string.Join(Environment.NewLine, new[]
        {
            "Como jogar:",
            "1. Informe um apelido (ou jogue como anônimo) e escolha a dificuldade.",
            "2. Leia cada pergunta e clique na alternativa que você acha correta.",
            "3. Se precisar, use a pista: cada pista usada custa 5 pontos.",
            "4. Cada acerto vale 10 pontos (fácil), 20 (médio) ou 30 (difícil).",
            "5. No final, veja o seu resumo e as marcas que apareceram na partida."
        });

        var imagemTitulo = CriarImagem("titulo.png", 650, 115);
        if (imagemTitulo != null) pnlInicio.Controls.Add(imagemTitulo);

        var imagemMascote = CriarImagem("mascote.png", 155, 155);
        if (imagemMascote != null) pnlInicio.Controls.Add(imagemMascote);

        pnlInicio.Controls.Add(Rotulo("ARCOR – Desafio das Marcas", 22, true));
        pnlInicio.Controls.Add(Rotulo("Descubra o quanto você conhece as marcas e os produtos da Arcor!", 12));
        pnlInicio.Controls.Add(Rotulo(instrucoes, 11));
        pnlInicio.Controls.Add(btnIniciar);
        pnlInicio.Controls.Add(btnHistInicio);

        btnIniciar.Click += (_, _) =>
        {
            txtNick.Clear();
            chkAnonimo.Checked = false;
            cboDif.SelectedIndex = 0;
            MostrarPainel(pnlIdent);
        };
        btnHistInicio.Click += async (_, _) => await MostrarHistoricoAsync();
    }

    private void MontarIdentificacao()
    {
        cboDif.Items.AddRange(new object[] { "Todas", "Fácil", "Médio", "Difícil" });
        cboDif.SelectedIndex = 0;

        pnlIdent.Controls.Add(Rotulo("Quem vai jogar?", 18, true));
        pnlIdent.Controls.Add(Rotulo("Apelido (nickname):", 11));
        txtNick.Margin = new Padding(0, 0, 0, 10);
        pnlIdent.Controls.Add(txtNick);
        pnlIdent.Controls.Add(chkAnonimo);
        pnlIdent.Controls.Add(Rotulo("Dificuldade das perguntas:", 11));
        pnlIdent.Controls.Add(cboDif);
        pnlIdent.Controls.Add(btnComecar);
        pnlIdent.Controls.Add(btnVoltarIdent);

        chkAnonimo.CheckedChanged += (_, _) => txtNick.Enabled = !chkAnonimo.Checked;
        btnComecar.Click += btnComecar_Click;
        btnVoltarIdent.Click += (_, _) => MostrarPainel(pnlInicio);
    }

    private void MontarQuiz()
    {
        pnlQuiz.Controls.Add(lblProgresso);
        pnlQuiz.Controls.Add(lblMarca);
        pnlQuiz.Controls.Add(lblEnunciado);

        for (int i = 0; i < _alts.Length; i++)
        {
            var b = new Button
            {
                Width = 680,
                Height = 50,
                Font = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 0, 8),
                Visible = false
            };
            b.Click += btnAlt_Click;
            _alts[i] = b;
            pnlQuiz.Controls.Add(b);
        }

        pnlQuiz.Controls.Add(btnPista);
        pnlQuiz.Controls.Add(lblPista);
        pnlQuiz.Controls.Add(lblResposta);
        pnlQuiz.Controls.Add(lblFeedback);
        pnlQuiz.Controls.Add(btnProxima);

        btnPista.Click += btnPista_Click;
        btnProxima.Click += btnProxima_Click;
    }

    private void MontarResultado()
    {
        pnlResultado.Controls.Add(Rotulo("Fim da partida!", 18, true));
        pnlResultado.Controls.Add(txtResumo);
        pnlResultado.Controls.Add(lblEnvio);
        pnlResultado.Controls.Add(btnReenviar);
        pnlResultado.Controls.Add(btnJogarNovamente);
        pnlResultado.Controls.Add(btnHistResultado);

        btnReenviar.Visible = false;
        btnReenviar.Click += async (_, _) => await EnviarResultadoAsync();
        btnJogarNovamente.Click += (_, _) => MostrarPainel(pnlInicio);
        btnHistResultado.Click += async (_, _) => await MostrarHistoricoAsync();
    }

    private void MontarHistorico()
    {
        pnlHistorico.Controls.Add(Rotulo("Histórico de partidas", 18, true));
        pnlHistorico.Controls.Add(txtHistorico);
        pnlHistorico.Controls.Add(btnVoltarHist);

        btnVoltarHist.Click += (_, _) => MostrarPainel(pnlInicio);
    }

    // =====================================================================
    // Inicio da partida: carrega as perguntas da API
    // =====================================================================
    private async void btnComecar_Click(object? sender, EventArgs e)
    {
        string nick = chkAnonimo.Checked ? "" : txtNick.Text.Trim();
        _nickname = nick.Length == 0 ? "Anônimo" : nick;

        string? dificuldade = cboDif.SelectedIndex > 0 ? cboDif.SelectedItem?.ToString() : null;

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
        _jogadas.Clear();
        _resultado = null;
        _cronometro.Restart();

        MostrarPergunta();
        MostrarPainel(pnlQuiz);
    }

    // =====================================================================
    // Quiz
    // =====================================================================
    private void MostrarPergunta()
    {
        var p = _perguntas[_indice];
        _respondida = false;
        _pistaUsada = false;

        lblMarca.Text = (string.IsNullOrWhiteSpace(p.Marca) ? "" : p.Marca + "   |   ") + "Dificuldade: " + p.Dificuldade;
        lblEnunciado.Text = p.Enunciado;
        lblPista.Text = "";
        lblResposta.Text = "";
        lblFeedback.Text = "";
        btnProxima.Visible = false;
        btnPista.Enabled = true;

        for (int i = 0; i < _alts.Length; i++)
        {
            var b = _alts[i];
            if (i < p.Alternativas.Count)
            {
                var alt = p.Alternativas[i];
                b.Text = alt.Letra + ") " + alt.Texto;
                b.Tag = alt.Letra;
                b.BackColor = Color.FromArgb(255, 244, 214);
                b.ForeColor = Color.FromArgb(74, 44, 26);
                b.UseVisualStyleBackColor = false;
                b.Visible = true;
            }
            else
            {
                b.Visible = false;
            }
        }

        AtualizarProgresso();
    }

    private int PontosAtuais()
    {
        int pontos = _jogadas.Where(j => j.Acertou).Sum(j => j.Nivel * 10) - _dicas * 5;
        return Math.Max(0, pontos);
    }

    private void AtualizarProgresso()
    {
        lblProgresso.Text = $"Pergunta {_indice + 1} de {_perguntas.Count}   |   Pontos: {PontosAtuais()}   |   Dicas usadas: {_dicas}";
    }

    private void btnAlt_Click(object? sender, EventArgs e)
    {
        if (_respondida || sender is not Button escolhido) return;
        _respondida = true;

        var p = _perguntas[_indice];
        string letraEscolhida = escolhido.Tag?.ToString() ?? "";
        bool acertou = letraEscolhida == p.RespostaCorreta;

        foreach (var b in _alts)
        {
            if (!b.Visible) continue;
            string letra = b.Tag?.ToString() ?? "";
            if (letra == p.RespostaCorreta) b.BackColor = Color.LightGreen;
            else if (b == escolhido) b.BackColor = Color.LightCoral;
        }

        _jogadas.Add((CalculadoraPartida.Nivel(p.Dificuldade), acertou));

        lblResposta.Text = acertou
            ? "Resposta correta!"
            : "Resposta incorreta. A correta era a alternativa " + p.RespostaCorreta + ".";
        lblResposta.ForeColor = acertou ? Color.DarkGreen : Color.DarkRed;

        string feedback = p.Feedback;
        if (!string.IsNullOrWhiteSpace(p.MarcaDescricao))
            feedback += Environment.NewLine + Environment.NewLine + "Sobre a marca " + p.Marca + ": " + p.MarcaDescricao;
        lblFeedback.Text = feedback;

        btnPista.Enabled = false;
        btnProxima.Text = _indice == _perguntas.Count - 1 ? "Ver resultado" : "Próxima pergunta";
        btnProxima.Visible = true;
        AtualizarProgresso();
    }

    private void btnPista_Click(object? sender, EventArgs e)
    {
        if (_respondida) return;

        var p = _perguntas[_indice];
        if (string.IsNullOrWhiteSpace(p.Pista))
        {
            lblPista.Text = "Esta pergunta não tem pista.";
            return;
        }

        // a pista so custa pontos na primeira vez que e aberta em cada pergunta
        if (!_pistaUsada)
        {
            _pistaUsada = true;
            _dicas++;
        }

        lblPista.Text = "Pista: " + p.Pista + "   (cada pista custa 5 pontos)";
        AtualizarProgresso();
    }

    private async void btnProxima_Click(object? sender, EventArgs e)
    {
        _indice++;
        if (_indice < _perguntas.Count) MostrarPergunta();
        else await FinalizarAsync();
    }

    // =====================================================================
    // Resultado final
    // =====================================================================
    private async Task FinalizarAsync()
    {
        _cronometro.Stop();

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
            TempoPartida = Math.Max(1, (int)Math.Round(_cronometro.Elapsed.TotalSeconds)),
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
    // Historico
    // =====================================================================
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
