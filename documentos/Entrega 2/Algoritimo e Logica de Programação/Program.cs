using System.Diagnostics;
using Spectre.Console;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ===================== BANCO DE QUESTÕES =====================
string[] faixasEtarias = {
    "Até 12 anos", "13 a 17 anos", "18 a 24 anos",
    "25 a 39 anos", "40 anos ou mais", "Prefiro não informar"
};
string[] enunciados = {
    // === FÁCEIS (Tortuguita e Block - Lúdico e Educacional) ===
    "Qual é a famosa personagem em formato de tartaruga que faz muito sucesso nos chocolates da Arcor?",
    "O chocolate Block da Arcor é conhecido principalmente por qual característica física?",
    "Além de saborosos, projetos modernos de sustentabilidade na indústria de doces incentivam o quê quanto às embalagens?",
    "O que significa consumir de forma consciente e apoiar marcas socialmente responsáveis?",
    "Qual é uma atitude simples de cidadania que podemos ter ao consumir um chocolate Tortuguita na rua?",
    "No contexto de educação alimentar, qual é a importância de saborear os doces com moderação?",
    "O chocolate Tortuguita é muito associado a qual momento divertido na infância?",
    "O amendoim presente no chocolate Block é uma oleaginosa que, na agricultura familiar, destaca-se por:",
    "As iniciativas de responsabilidade social corporativa em grandes marcas de chocolate visam principalmente:",
    "Qual é o cuidado essencial que devemos ter com a embalagem metalizada ou plástica de um chocolate Block após o consumo?",

    // === DIFÍCEIS (Tortuguita e Block - Técnico e Aprofundado) ===
    "A linha Tortuguita da Arcor é amplamente reconhecida pela diversidade de recheios. Qual destes ingredientes NÃO costuma fazer parte dos recheios clássicos das versões tradicionais?",
    "No cérebro humano, o consumo moderado de cacau presente em chocolates como o Block estimula a libertação de neurotransmissores associados a:",
    "Como as práticas de ESG (Ambiental, Social e Governança) influenciam a cadeia produtiva de marcas de chocolate como Tortuguita e Block?",
    "No desenvolvimento de software sustentável (Green Software Engineering), qual é uma meta prioritária para aplicações de quiz focadas em marcas de doces?",
    "Qual é o principal objetivo do conceito de economia circular aplicado à produção e descarte de embalagens de confectionery?",
    "Na modelagem de dados relacional para gerenciar o ranking de pontuações de um quiz sobre Tortuguita e Block, a normalização serve para:",
    "O conceito de consumo ético e sustentável aborda diretamente a responsabilidade do consumidor em:",
    "No desenvolvimento em C# com .NET para a aplicação deste quiz, a separação de responsabilidades entre Frontend e Backend promove:",
    "Qual destas ações representa um pilar fundamental da responsabilidade social empresarial (RSE) ligada a marcas de grande consumo?",
    "Na agricultura sustentável de matérias-primas utilizadas em chocolates (como o cacau e o amendoim do Block), a certificação de comércio justo (Fair Trade) garante:"
};

string[] niveis = { 
    "F", "F", "F", "F", "F", "F", "F", "F", "F", "F", 
    "D", "D", "D", "D", "D", "D", "D", "D", "D", "D" 
};

string[] dicas = {
    // Fáceis
    "Pense no animal que tem um casco duro e uma cabecinha simpática.",
    "É um tablete robusto com pedaços crocantes misturados.",
    "O foco é sempre preservar o meio ambiente reduzindo o impacto.",
    "Envolve olhar além do preço e pensar na ética da produção.",
    "Lugar de lixo é na lixeira ou ecoponto.",
    "Equilíbrio é a chave para uma vida saudável e feliz.",
    "Envolve diversão, piadas e interatividade com o casco da tartaruga.",
    "Pense no impacto social positivo para quem cultiva no campo.",
    "O foco é o bem-estar coletivo, o meio ambiente e os colaboradores.",
    "Nunca deixe o lixo espalhado pela natureza ou calçada.",

    // Difíceis
    "Pense em algo totalmente salgado e totalmente incomum em recheios doces.",
    "Chocolate costuma trazer aquela sensação boa de bem-estar e foco.",
    "ESG busca rastreabilidade de ingredientes e sustentabilidade nas fábricas.",
    "Envolve eficiência energética e otimização digital do código.",
    "A ideia é reaproveitar e manter os materiais em ciclo pelo maior tempo.",
    "Evita duplicações desnecessárias e inconsistências no banco de dados.",
    "O consumidor tem voz ativa ao exigir transparência e respeito ecológico.",
    "Ajuda na organização, testes e escalabilidade do código C#.",
    "Envolve investir em projetos de educação e inclusão comunitária.",
    "Garante preços justos e dignidade para os produtores rurais."
};

string[,] alternativas = {
    // Fáceis
    { "Tortuguita", "Melocoton", "Pinguim", "Elefante" },
    { "Por ser recheado de morango líquido", "Por ser um tablete grande e recheado com pedaços crocantes de amendoim", "Por ser totalmente branco sem cacau", "Por vir em formato de palito" },
    { "O descarte de plástico nos oceanos", "A utilização de materiais recicláveis e a redução do impacto ambiental", "O aumento do uso de embalagens não recicláveis", "A proibição total de qualquer tipo de papel" },
    { "Comprar apenas o mais barato sem importar a origem", "Considerar o impacto ambiental, o apoio à comunidade e a ética da empresa produtora", "Evitar comprar qualquer tipo de alimento industrializado", "Comprar apenas produtos importados" },
    { "Jogar a embalagem no chão", "Descartar o invólucro corretamente na lixeira ou ecoponto mais próximo para reciclagem", "Esconder a embalagem debaixo do banco da praça", "Deixar em cima da mesa sem se importar" },
    { "Manter um estilo de vida equilibrado e saudável aliado a bons hábitos", "Substituir todas as refeições principais por chocolate", "Evitar completamente a prática de qualquer exercício físico", "Não existe nenhuma importância" },
    { "Aos jogos de computador de terror extremo", "Às brincadeiras, piadas e interação lúdica com o casco e a cabecinha da tartaruga", "Aos estudos complexos de matemática financeira", "Aos testes de química avançada" },
    { "Ser cultivado exclusivamente em desertos gelados", "Gerar emprego, renda e sustento para muitos pequenos produtores rurais locais", "Não precisar de nenhum tipo de solo para crescer", "Ser produzido apenas em laboratório sintético" },
    { "Aumentar a poluição dos rios locais de propósito", "Contribuir para o desenvolvimento sustentável e o bem-estar da sociedade e dos colaboradores", "Reduzir drasticamente o salário dos funcionários", "Ignorar as leis trabalhistas e ambientais vigentes" },
    { "Jogar no rio para a água levar", "Descartar corretamente na coleta seletiva para reciclagem", "Queimar em quintal aberto", "Enterrar no jardim da escola" },

    // Difíceis
    { "Baunilha", "Brigadeiro", "Carne bovina desidratada", "Morango" },
    { "Aumento imediato de fadiga extrema", "Sensação de bem-estar, foco e prazer (como a serotonina e dopamina)", "Perda total de memória a curto prazo", "Aceleração de doenças respiratórias" },
    { "Apenas criando burocracia sem alterar o produto", "Garantindo rastreabilidade dos ingredientes, comércio justo e redução de emissões nas fábricas", "Incentivando o trabalho infantil nas lavouras", "Ignorando totalmente a pegada ecológica corporativa" },
    { "Aumentar o consumo desnecessário de energia dos servidores", "Otimizar o código e reduzir a pegada de carbono computacional e o desperdício de recursos digitais", "Deixar o sistema o mais lento possível", "Criar loops infinitos de processamento" },
    { "Extrair, produzir, usar e descartar o mais rápido possível", "Manter os produtos, componentes e materiais em circulação pelo maior tempo possível, eliminando resíduos", "Aumentar a quantidade de aterros sanitários nas cidades", "Queimar todos os resíduos plásticos em praça pública" },
    { "Duplicar dados propositadamente para deixar o sistema mais lento", "Evitar redundâncias anómalas, garantir a integridade referencial e otimizar a consistência dos dados", "Apagar o banco de dados toda vez que o sistema for reiniciado", "Impedir que qualquer pessoa consiga ler as perguntas" },
    { "Exigir que as empresas adotem transparência, direitos laborais justos e respeito ecológico", "Comprar apenas produtos que prejudicam o meio ambiente", "Incentivar o monopólio de grandes multinacionais poluidoras", "Ignorar de onde vêm os produtos que consome" },
    { "Acoplamento total e impossibilidade de manutenção", "Modularidade, facilidade de testes unitários, manutenção limpa e escalabilidade do software", "Erros obrigatórios de compilação em todas as execuções", "Geração intencional de conflitos no Git" },
    { "Investir em projetos de educação, inclusão comunitária e valorização da diversidade local", "Cortar todos os apoios a projetos educacionais e culturais", "Reduzir a qualidade nutricional dos alimentos intencionalmente", "Aumentar os custos dos produtos básicos para excluir comunidades carentes" },
    { "Condições dignas de trabalho, preço justo e sustentabilidade para os agricultores", "Exploração excessiva da terra sem descanso", "Uso proibido de água em plantações", "Incentivo ao êxodo rural desordenado" }
};

int[] corretas = { 
    0, 1, 1, 1, 1, 0, 1, 1, 1, 1, // Fáceis (0=A, 1=B, 2=C, 3=D)
    2, 1, 1, 1, 1, 1, 0, 1, 0, 0  // Difíceis
};

// ===================== INÍCIO DO JOGO =====================
MostrarAbertura();
string nickname = LerNickname();
string faixa = LerFaixaEtaria(faixasEtarias);

// PROVISÓRIO: apagar no início do Bloco 3
//for (int i = 0; i < enunciados.Length; i++)
//{
//    string[] opcoes = ExtrairAlternativas(alternativas, i);
//    string nivel = niveis[i];
//    string pergunta = Markup.Escape(enunciados[i]);
//    string respCorreta = Markup.Escape(opcoes[corretas[i]]);
//    AnsiConsole.MarkupLine($"[green]{nivel}[/] {pergunta} -> {respCorreta}");
//}

int totalQuestoes = enunciados.Length;
int[] questoesPorNivel = new int[3];
int[] acertosPorNivel = new int[3];
bool[] usouDica = new bool[totalQuestoes];
bool terminou = false;

Stopwatch cronometro = Stopwatch.StartNew();

for (int i = 0; i < totalQuestoes; i++)
{

    int segundos = (int)cronometro.Elapsed.TotalSeconds;
    int minutos = segundos / 60;
    int segs = segundos % 60;
    AnsiConsole.Write(new Rule($"Questão {i + 1} de {totalQuestoes} | Tempo: {minutos} min {segs} s"));

    string[] opcoes = ExtrairAlternativas(alternativas, i);
    int resultado = MostrarQuestao(enunciados[i], opcoes, corretas[i], dicas[i], usouDica, i);

    if (resultado == -1)
    {
        terminou = true;
        break;
    }

    int nivel = IndiceNivel(niveis[i]);
    questoesPorNivel[nivel]++;

    if (resultado == 1)
    {
        acertosPorNivel[nivel]++;
        AnsiConsole.MarkupLine("[green]Você acertou![/]");
    }
    else
    {
        AnsiConsole.MarkupLine("[red]Você errou.[/] Resposta certa: " + Markup.Escape(opcoes[corretas[i]]));
    }
}

cronometro.Stop();
if (terminou)
{
    AnsiConsole.MarkupLine("[yellow]Quiz encerrado pelo jogador. Até a próxima![/]");
}
else
{
int tempo = (int)cronometro.Elapsed.TotalSeconds;
int dicasUsadas = ContarDicas(usouDica);
MostrarRelatorio(nickname, faixa, questoesPorNivel, acertosPorNivel, dicasUsadas, tempo);
}

// ===================== FUNÇÕES =====================

static void MostrarAbertura()
{
    Console.Clear();
    AnsiConsole.Write(new FigletText("Desafio das Marcas").Color(Color.Green));
    AnsiConsole.MarkupLine("[yellow]Bem-vindo ao quiz da Arcor![/]");
    AnsiConsole.MarkupLine("Use as setas para escolher a resposta e tecle Enter.");
    AnsiConsole.MarkupLine("Cada dica usada custa 5 pontos. Escolha TERMINAR para sair a qualquer momento.");
    Console.WriteLine();
}

static string LerNickname()
{
    return AnsiConsole.Ask<string>("Digite seu [green]nickname[/]:");
}

static string LerFaixaEtaria(string[] faixas)
{
    SelectionPrompt<string> prompt = new SelectionPrompt<string>();
    prompt.Title("Escolha sua [green]faixa etária[/]:");
    prompt.AddChoices(faixas);
    return AnsiConsole.Prompt(prompt);
}

static string[] ExtrairAlternativas(string[,] alternativas, int linha)
{
    int quantidade = alternativas.GetLength(1);
    string[] opcoes = new string[quantidade];
    for (int j = 0; j < quantidade; j++)
    {
        opcoes[j] = alternativas[linha,j];
    }
    return opcoes;
}

static int MostrarQuestao(string enunciado, string[] opcoes, int correta, string dica, bool[] usouDica, int indice)
{
    SelectionPrompt<string> prompt = new SelectionPrompt<string>();
    prompt.Title(Markup.Escape(enunciado));
    prompt.AddChoices(opcoes);
    prompt.AddChoices("MOSTRAR DICA", "TERMINAR");

    string escolha = AnsiConsole.Prompt(prompt);

    while (escolha == "MOSTRAR DICA")
    {
        usouDica[indice] = true;
        AnsiConsole.Status().Start("Buscando a dica...", ctx =>
        {
            Thread.Sleep(1500);
        });
        AnsiConsole.MarkupLine("[yellow]Dica:[/] " + Markup.Escape(dica));
        escolha = AnsiConsole.Prompt(prompt);
    }

    if (escolha == "TERMINAR")
    {
        return -1;
    }
    if (escolha == opcoes[correta])
    {
        return 1;
    }
    return 0;
}

static int IndiceNivel(string nivel)
{
    if (nivel == "F")
    {
        return 0;
    }
    else if (nivel == "M")
    {
        return 1;
    }
    return 2;
}

static int ContarDicas(bool[] usouDica)
{
    int total = 0;
    for (int i = 0; i < usouDica.Length; i++)
    {
        if (usouDica[i])
        {
            total++;
        }
    }
    return total;
}

static double Percentual(int acertos, int questoes)
{
    return acertos * 100.0 / questoes;
}

static int CalcularPontuacao(int[] acertosPorNivel, int dicas)
{
    int[] pesos = { 10, 20, 30 };
    int pontos = 0;
    for (int n = 0; n < 3; n++)
    {
        pontos = pontos + acertosPorNivel[n] * pesos[n];
    }
    pontos = pontos - dicas * 5;
    if (pontos < 0)
    {
        pontos = 0;
    }
    return pontos;
}

static int CalcularPontuacaoMaxima(int[] questoesPorNivel)
{
    int[] pesos = { 10, 20, 30 };
    int maximo = 0;
    for (int n = 0; n < 3; n++)
    {
        maximo = maximo + questoesPorNivel[n] * pesos[n];
    }
    return maximo;
}

static string Classificar(double percentual)
{
    if (percentual >= 90.0)
    {
        return "Mestre das Marcas";
    }
    else if (percentual >= 70.0)
    {
        return "Conhecedor de Marcas";
    }
    else if (percentual >= 50.0)
    {
        return "Aprendiz";
    }
    return "Iniciante";
}

static string GerarBarra(int acertos)
{
    string barra = "";
    for (int i = 0; i < acertos; i++)
    {
        barra = barra + "";
    }
    return barra;
}

static string DefinirRitmo(double mediaPorQuestao)
{
    // DESAFIO A (opcional): devolva "Rápido" (até 10 s), "Normal" (até 20 s)
    // ou "Pausado" (acima de 20 s), com as mesmas regras da Entrega 1.
    return "(não calculado)";
}

static string MelhorNivel(int[] questoesPorNivel, int[] acertosPorNivel, string[] nomesNiveis)
{
    // DESAFIO B (opcional): percorra os 3 níveis com um for, ignore os níveis
    // não jogados e devolva o nome do nível de maior percentual.
    // Em caso de empate, vence o nível mais difícil.
    // Dica: use a função Percentual e o vetor nomesNiveis.
    return "(não calculado)";
}

static void MostrarRelatorio(string nickname, string faixa, int[] questoesPorNivel, int[] acertosPorNivel, int dicas, int tempo)
{
    string[] nomesNiveis = { "Fácil", "Médio", "Difícil" };

    int totalQuestoes = 0;
    int totalAcertos = 0;
    for (int n = 0; n < 3; n++)
    {
        totalQuestoes = totalQuestoes + questoesPorNivel[n];
        totalAcertos = totalAcertos + acertosPorNivel[n];
    }

    double percentualGeral = Percentual(totalAcertos, totalQuestoes);
    int pontos = CalcularPontuacao(acertosPorNivel, dicas);
    int maximo = CalcularPontuacaoMaxima(questoesPorNivel);
    double media = tempo * 1.0 / totalQuestoes;

    Console.WriteLine();
    Console.WriteLine("===== ARCOR – DESAFIO DAS MARCAS: RESUMO DA PARTIDA =====");
    Console.WriteLine("Jogador: " + nickname);
    Console.WriteLine("Faixa etária: " + faixa);
    Console.WriteLine();
    Console.WriteLine("Desempenho por nível:");
    for (int n = 0; n < 3; n++)
    {
        if (questoesPorNivel[n] == 0)
        {
            Console.WriteLine($"{nomesNiveis[n],-8}(0/0)  não jogado");
        }
        else
        {
            double p = Percentual(acertosPorNivel[n], questoesPorNivel[n]);
            Console.WriteLine($"{nomesNiveis[n],-8}({acertosPorNivel[n]}/{questoesPorNivel[n]})  {p,5:F1}%  {GerarBarra(acertosPorNivel[n])}");
        }
    }
    Console.WriteLine();
    Console.WriteLine($"Total: {totalAcertos} acertos e {totalQuestoes - totalAcertos} erros em {totalQuestoes} questões ({percentualGeral:F1}%)");
    Console.WriteLine($"Pontuação: {pontos} de {maximo} pontos possíveis ({pontos * 100.0 / maximo:F1}%)");
    Console.WriteLine($"Dicas usadas: {dicas} (penalidade de {dicas * 5} pontos)");
    Console.WriteLine($"Tempo total: {tempo / 60} min {tempo % 60} s | Média: {media:F1} s por questão");
    Console.WriteLine("Ritmo: " + DefinirRitmo(media));
    Console.WriteLine("Melhor nível: " + MelhorNivel(questoesPorNivel, acertosPorNivel, nomesNiveis));
    Console.WriteLine("Classificação: " + Classificar(percentualGeral));
    Console.WriteLine("=========================================================");
}