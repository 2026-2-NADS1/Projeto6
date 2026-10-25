using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace QuizArcor;

// =====================================================================
// Cores, fontes e carregamento de imagens
// =====================================================================
public static class Tema
{
    public static readonly Color Amarelo = Color.FromArgb(255, 196, 0);
    public static readonly Color Azul = Color.FromArgb(30, 90, 168);
    public static readonly Color AzulEscuro = Color.FromArgb(18, 56, 112);
    public static readonly Color Marrom = Color.FromArgb(74, 44, 26);
    public static readonly Color MarromClaro = Color.FromArgb(122, 74, 43);
    public static readonly Color Creme = Color.FromArgb(255, 244, 214);
    public static readonly Color Verde = Color.FromArgb(62, 155, 90);
    public static readonly Color Vermelho = Color.FromArgb(228, 87, 46);

    private static readonly Dictionary<string, Image?> _cache = new();

    // Carrega Imagens\<nome> (pasta ao lado do programa). Se nao existir, devolve null
    public static Image? Imagem(string nome)
    {
        if (_cache.TryGetValue(nome, out var guardada)) return guardada;

        Image? carregada = null;
        try
        {
            string caminho = Path.Combine(AppContext.BaseDirectory, "Imagens", nome);
            if (File.Exists(caminho))
            {
                using var fs = File.OpenRead(caminho);
                using var temp = Image.FromStream(fs);
                carregada = new Bitmap(temp);   // copia, para nao travar o arquivo
            }
        }
        catch
        {
            carregada = null;
        }

        _cache[nome] = carregada;
        return carregada;
    }

    public static Font Fonte(float tamanho, FontStyle estilo = FontStyle.Bold)
    {
        return new Font("Segoe UI", tamanho, estilo, GraphicsUnit.Point);
    }

    public static bool ContemMarca(string? marca, string nome)
    {
        return marca != null && marca.Contains(nome, StringComparison.OrdinalIgnoreCase);
    }

    public static GraphicsPath Arredondado(Rectangle r, int raio)
    {
        int d = Math.Max(2, raio * 2);
        var caminho = new GraphicsPath();
        caminho.AddArc(r.X, r.Y, d, d, 180, 90);
        caminho.AddArc(r.X + r.Width - d, r.Y, d, d, 270, 90);
        caminho.AddArc(r.X + r.Width - d, r.Y + r.Height - d, d, d, 0, 90);
        caminho.AddArc(r.X, r.Y + r.Height - d, d, d, 90, 90);
        caminho.CloseFigure();
        return caminho;
    }

    public static Color Misturar(Color cor, Color alvo, float t)
    {
        return Color.FromArgb(cor.A,
            (int)(cor.R + (alvo.R - cor.R) * t),
            (int)(cor.G + (alvo.G - cor.G) * t),
            (int)(cor.B + (alvo.B - cor.B) * t));
    }

    // Desenha a imagem inteira dentro do retangulo, sem distorcer
    public static void DesenharAjustado(Graphics g, Image img, Rectangle destino)
    {
        float escala = Math.Min((float)destino.Width / img.Width, (float)destino.Height / img.Height);
        float w = img.Width * escala;
        float h = img.Height * escala;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.DrawImage(img, destino.X + (destino.Width - w) / 2f, destino.Y + (destino.Height - h) / 2f, w, h);
    }
}

// =====================================================================
// Painel com imagem de fundo (preenche a janela sem distorcer) e "flash" colorido
// =====================================================================
public class PainelImagem : Panel
{
    private Image? _imagem;
    private Color _cor1 = Tema.Azul;
    private Color _cor2 = Tema.AzulEscuro;
    private Color _flash = Color.Transparent;
    private Bitmap? _pronto;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Image? Imagem
    {
        get => _imagem;
        set { _imagem = value; Descartar(); }
    }

    // Cores do degrade usado quando nao ha imagem
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color CorFundo1
    {
        get => _cor1;
        set { _cor1 = value; Descartar(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color CorFundo2
    {
        get => _cor2;
        set { _cor2 = value; Descartar(); }
    }

    // Camada colorida semitransparente por cima do fundo (usada no acerto/erro)
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color Flash
    {
        get => _flash;
        set { _flash = value; Invalidate(); }
    }

    public PainelImagem()
    {
        DoubleBuffered = true;
    }

    private void Descartar()
    {
        _pronto?.Dispose();
        _pronto = null;
        Invalidate();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        Descartar();
        base.OnSizeChanged(e);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // tudo e desenhado no OnPaint
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width <= 0 || Height <= 0) return;

        _pronto ??= Montar();
        e.Graphics.DrawImageUnscaled(_pronto, 0, 0);

        if (_flash.A > 0)
        {
            using var pincel = new SolidBrush(_flash);
            e.Graphics.FillRectangle(pincel, e.ClipRectangle);
        }

        base.OnPaint(e);
    }

    // Prepara o fundo uma unica vez (assim a animacao nao precisa redimensionar a imagem a cada quadro)
    private Bitmap Montar()
    {
        var bmp = new Bitmap(Width, Height);
        using var g = Graphics.FromImage(bmp);

        if (_imagem != null)
        {
            float escala = Math.Max((float)Width / _imagem.Width, (float)Height / _imagem.Height);
            float w = _imagem.Width * escala;
            float h = _imagem.Height * escala;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(_imagem, (Width - w) / 2f, (Height - h) / 2f, w, h);
        }
        else
        {
            using var degrade = new LinearGradientBrush(new Rectangle(0, 0, Width, Height), _cor1, _cor2, 90f);
            g.FillRectangle(degrade, 0, 0, Width, Height);
        }

        return bmp;
    }
}

// =====================================================================
// Cartao arredondado com sombra (pode mostrar uma imagem na lateral)
// =====================================================================
public class PainelCartao : Panel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color Preenchimento { get; set; } = Tema.Creme;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color Borda { get; set; } = Tema.Marrom;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Raio { get; set; } = 22;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Sombra { get; set; } = 6;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Image? ImagemLateral { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int LarguraImagem { get; set; }

    public PainelCartao()
    {
        SetStyle(ControlStyles.UserPaint
                 | ControlStyles.AllPaintingInWmPaint
                 | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw
                 | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var area = new Rectangle(2, 2, Width - 6, Height - Sombra - 5);

        using (var sombra = Tema.Arredondado(new Rectangle(area.X, area.Y + Sombra, area.Width, area.Height), Raio))
        using (var pincelSombra = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
        {
            g.FillPath(pincelSombra, sombra);
        }

        using (var caminho = Tema.Arredondado(area, Raio))
        using (var pincel = new SolidBrush(Preenchimento))
        using (var caneta = new Pen(Borda, 3))
        {
            g.FillPath(pincel, caminho);
            g.DrawPath(caneta, caminho);
        }

        if (ImagemLateral != null && LarguraImagem > 0)
        {
            var destino = new Rectangle(area.X + 16, area.Y + 12, LarguraImagem, area.Height - 24);
            Tema.DesenharAjustado(g, ImagemLateral, destino);
        }

        base.OnPaint(e);
    }
}

// =====================================================================
// Botao colorido estilo "jogo" (cantos arredondados, efeito 3D, simbolo opcional)
// =====================================================================
public enum Forma
{
    Nenhuma,
    Triangulo,
    Losango,
    Circulo,
    Quadrado
}

public class BotaoJogo : Control
{
    private Color _cor = Tema.Azul;
    private Color _corTexto = Color.White;
    private Forma _simbolo = Forma.Nenhuma;
    private bool _sobre;
    private bool _pressionado;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color Cor
    {
        get => _cor;
        set { _cor = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color CorTexto
    {
        get => _corTexto;
        set { _corTexto = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Forma Simbolo
    {
        get => _simbolo;
        set { _simbolo = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Raio { get; set; } = 16;

    public BotaoJogo()
    {
        SetStyle(ControlStyles.UserPaint
                 | ControlStyles.AllPaintingInWmPaint
                 | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw
                 | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        TabStop = false;
        Font = Tema.Fonte(12);
        Size = new Size(200, 48);
    }

    protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
    protected override void OnMouseEnter(EventArgs e) { _sobre = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _sobre = false; _pressionado = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) { _pressionado = true; Invalidate(); }
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _pressionado = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        const int profundidade = 5;
        int descida = _pressionado ? profundidade - 2 : 0;

        Color corBase = Enabled ? _cor : Color.FromArgb(150, 150, 150);
        if (_sobre && Enabled && !_pressionado) corBase = Tema.Misturar(corBase, Color.White, 0.15f);
        Color corSombra = Tema.Misturar(corBase, Color.Black, 0.35f);

        var area = new Rectangle(1, 1, Width - 3, Height - profundidade - 3);
        var rSombra = new Rectangle(area.X, area.Y + profundidade, area.Width, area.Height);
        var rFace = new Rectangle(area.X, area.Y + descida, area.Width, area.Height);

        using (var caminhoSombra = Tema.Arredondado(rSombra, Raio))
        using (var pincelSombra = new SolidBrush(corSombra))
        {
            g.FillPath(pincelSombra, caminhoSombra);
        }

        using (var caminhoFace = Tema.Arredondado(rFace, Raio))
        using (var pincelFace = new SolidBrush(corBase))
        using (var contorno = new Pen(Tema.Misturar(corBase, Color.Black, 0.5f), 2))
        {
            g.FillPath(pincelFace, caminhoFace);
            g.DrawPath(contorno, caminhoFace);
        }

        var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak
                    | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
        int margemEsq = 12;

        if (_simbolo != Forma.Nenhuma)
        {
            int lado = Math.Min(rFace.Height - 28, 46);
            var rSimbolo = new Rectangle(rFace.X + 20, rFace.Y + (rFace.Height - lado) / 2, lado, lado);
            DesenharForma(g, rSimbolo);
            margemEsq = 20 + lado + 16;
            flags |= TextFormatFlags.Left;
        }
        else
        {
            flags |= TextFormatFlags.HorizontalCenter;
        }

        var rTexto = new Rectangle(rFace.X + margemEsq, rFace.Y, rFace.Width - margemEsq - 14, rFace.Height);
        TextRenderer.DrawText(g, Text, Font, rTexto, Enabled ? _corTexto : Color.WhiteSmoke, flags);
    }

    private void DesenharForma(Graphics g, Rectangle r)
    {
        using var pincel = new SolidBrush(_corTexto);
        switch (_simbolo)
        {
            case Forma.Triangulo:
                g.FillPolygon(pincel, new[]
                {
                    new Point(r.X + r.Width / 2, r.Y),
                    new Point(r.X + r.Width, r.Y + r.Height),
                    new Point(r.X, r.Y + r.Height)
                });
                break;
            case Forma.Losango:
                g.FillPolygon(pincel, new[]
                {
                    new Point(r.X + r.Width / 2, r.Y),
                    new Point(r.X + r.Width, r.Y + r.Height / 2),
                    new Point(r.X + r.Width / 2, r.Y + r.Height),
                    new Point(r.X, r.Y + r.Height / 2)
                });
                break;
            case Forma.Circulo:
                g.FillEllipse(pincel, r);
                break;
            case Forma.Quadrado:
                g.FillRectangle(pincel, r);
                break;
        }
    }
}

// =====================================================================
// Barra de tempo (verde -> amarelo -> vermelho conforme o tempo acaba)
// =====================================================================
public class BarraTempo : Control
{
    private float _valor = 1f;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public float Valor
    {
        get => _valor;
        set { _valor = Math.Clamp(value, 0f, 1f); Invalidate(); }
    }

    public BarraTempo()
    {
        SetStyle(ControlStyles.UserPaint
                 | ControlStyles.AllPaintingInWmPaint
                 | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw
                 | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Size = new Size(400, 16);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int raio = Height / 2;
        var trilha = new Rectangle(0, 0, Width - 1, Height - 1);

        using (var caminho = Tema.Arredondado(trilha, raio))
        using (var pincel = new SolidBrush(Color.FromArgb(110, 0, 0, 0)))
        {
            g.FillPath(pincel, caminho);
        }

        int largura = (int)((Width - 5) * _valor);
        if (largura >= 4)
        {
            Color cor = _valor > 0.5f ? Tema.Verde : _valor > 0.25f ? Tema.Amarelo : Tema.Vermelho;
            var preenchido = new Rectangle(2, 2, largura, Height - 5);
            using var caminho = Tema.Arredondado(preenchido, Math.Min(raio - 2, largura / 2));
            using var pincel = new SolidBrush(cor);
            g.FillPath(pincel, caminho);
        }
    }
}
