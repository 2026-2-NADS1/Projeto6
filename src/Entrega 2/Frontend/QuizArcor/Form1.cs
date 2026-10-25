using System.Text;

namespace QuizArcor;

public class Form1 : Form
{
    private readonly QuizApiService _api = new();
    private ResultadoPartida? _resultado;

    private static NumericUpDown Num() => new() { Minimum = 0, Maximum = 100000, Width = 100 };

    private readonly TextBox txtNickname = new() { Width = 220 };
    private readonly ComboBox cboFaixa = new() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown numFaceis = Num(), numAcertosFaceis = Num();
    private readonly NumericUpDown numMedias = Num(), numAcertosMedias = Num();
    private readonly NumericUpDown numDificeis = Num(), numAcertosDificeis = Num();
    private readonly NumericUpDown numTempo = Num(), numDicas = Num();
    private readonly Button btnCalcular = new() { Text = "Calcular", AutoSize = true };
    private readonly Button btnEnviar = new() { Text = "Enviar ao servidor", AutoSize = true, Enabled = false };
    private readonly TextBox txtResumo = new()
    {
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Font = new Font("Consolas", 10),
        Dock = DockStyle.Fill
    };

    public Form1()
    {
        Text = "Arcor – Desafio das Marcas";
        ClientSize = new Size(660, 780);
        StartPosition = FormStartPosition.CenterScreen;

        cboFaixa.Items.AddRange(new object[]
        {
            "Até 12 anos", "13 a 17 anos", "18 a 24 anos",
            "25 a 39 anos", "40 anos ou mais", "Prefiro não informar"
        });
        cboFaixa.SelectedIndex = 0;

        var grade = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(10)
        };

        void Linha(string rotulo, Control controle)
        {
            grade.Controls.Add(new Label
            {
                Text = rotulo,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 8, 10, 3)
            });
            grade.Controls.Add(controle);
        }

        Linha("Nickname:", txtNickname);
        Linha("Faixa etária:", cboFaixa);
        Linha("Questões fáceis apresentadas:", numFaceis);
        Linha("Questões fáceis acertadas:", numAcertosFaceis);
        Linha("Questões médias apresentadas:", numMedias);
        Linha("Questões médias acertadas:", numAcertosMedias);
        Linha("Questões difíceis apresentadas:", numDificeis);
        Linha("Questões difíceis acertadas:", numAcertosDificeis);
        Linha("Tempo total (segundos):", numTempo);
        Linha("Dicas usadas:", numDicas);

        var botoes = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
        botoes.Controls.Add(btnCalcular);
        botoes.Controls.Add(btnEnviar);

        btnCalcular.Click += btnCalcular_Click;
        btnEnviar.Click += btnEnviar_Click;

        // A ordem importa: o controle que ocupa o resto (Fill) e adicionado primeiro
        Controls.Add(txtResumo);
        Controls.Add(botoes);
        Controls.Add(grade);
    }

    private void btnCalcular_Click(object? sender, EventArgs e)
    {
        var dados = new DadosPartida
        {
            Nickname = txtNickname.Text.Trim(),
            FaixaEtaria = cboFaixa.SelectedIndex + 1,
            QuestoesFaceis = (int)numFaceis.Value,
            AcertosFaceis = (int)numAcertosFaceis.Value,
            QuestoesMedias = (int)numMedias.Value,
            AcertosMedias = (int)numAcertosMedias.Value,
            QuestoesDificeis = (int)numDificeis.Value,
            AcertosDificeis = (int)numAcertosDificeis.Value,
            TempoPartida = (int)numTempo.Value,
            DicasUsadas = (int)numDicas.Value
        };

        var erros = CalculadoraPartida.Validar(dados);
        if (erros.Count > 0)
        {
            MessageBox.Show(string.Join(Environment.NewLine, erros),
                "Dados inválidos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            btnEnviar.Enabled = false;
            return;
        }

        _resultado = CalculadoraPartida.Calcular(dados);
        txtResumo.Text = MontarResumo(_resultado, dados);
        btnEnviar.Enabled = true;
    }

    private async void btnEnviar_Click(object? sender, EventArgs e)
    {
        if (_resultado == null) return;

        btnEnviar.Enabled = false;
        try
        {
            string resposta = await _api.EnviarResultadoAsync(_resultado);
            string trecho = resposta.Length > 600 ? resposta[..600] + "..." : resposta;
            MessageBox.Show("Resultado enviado com sucesso!\n" + resposta);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            MessageBox.Show("Falha ao falar com o servidor: " + ex.Message,
                "Erro de rede", MessageBoxButtons.OK, MessageBoxIcon.Error);
            btnEnviar.Enabled = true;
        }
    }

    private static string MontarResumo(ResultadoPartida r, DadosPartida d)
    {
        string Linha(string nome, int acertos, int questoes, double pct) =>
            questoes > 0
                ? $"{nome} ({acertos}/{questoes})  {pct:F1}%  {new string('*', acertos)}"
                : $"{nome} não jogado";

        var sb = new StringBuilder();
        sb.AppendLine("===== ARCOR – DESAFIO DAS MARCAS: RESUMO DA PARTIDA =====");
        sb.AppendLine($"Jogador: {r.Nickname}");
        sb.AppendLine($"Faixa etária: {r.NomeFaixa}");
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
        return sb.ToString();
    }
}
