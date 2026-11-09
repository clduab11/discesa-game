using UnityEngine;

/// <summary>
/// Original procedural HUD art. Everything is drawn in code with a fixed deterministic
/// pattern (no UnityEngine.Random), so builds and captures look identical every run and
/// no third-party texture enters the repository. Textures are cached for the session.
///
/// M4 may replace these with Unity AI generated art; the HUD keeps working either way
/// because it only asks HudArt for sprites.
/// </summary>
public static class HudArt
{
    static Sprite _white;
    static Sprite _cross;
    static Sprite _primaryBracket;
    static Sprite _secondaryBracket;
    static Sprite _vignette;
    static Sprite _portrait;
    static Sprite _panel;
    static Sprite _panelDark;
    static Sprite _console;
    static Sprite _pillarLeft;
    static Sprite _pillarRight;
    static Sprite _segments;
    static readonly Sprite[] WeaponIcons = new Sprite[3];

    public static Sprite White => _white != null ? _white : (_white = MakeSolid(4, 4, Color.white));

    public static Sprite Panel =>
        _panel != null ? _panel : (_panel = MakePanel(96, 96, new Color(0.16f, 0.19f, 0.24f), new Color(0.42f, 0.48f, 0.55f)));

    public static Sprite PanelDark =>
        _panelDark != null ? _panelDark : (_panelDark = MakePanel(96, 96, new Color(0.07f, 0.09f, 0.12f), new Color(0.26f, 0.30f, 0.36f)));

    public static Sprite Cross =>
        _cross != null ? _cross : (_cross = MakeCross(64));

    public static Sprite PrimaryBracket =>
        _primaryBracket != null ? _primaryBracket : (_primaryBracket = MakeBracket(40, 2, new Color(0.55f, 1f, 0.65f, 0.9f)));

    public static Sprite SecondaryBracket =>
        _secondaryBracket != null ? _secondaryBracket : (_secondaryBracket = MakeBracket(56, 1, new Color(1f, 0.72f, 0.32f, 0.75f)));

    public static Sprite Vignette =>
        _vignette != null ? _vignette : (_vignette = MakeVignette(256));

    public static Sprite Portrait =>
        _portrait != null ? _portrait : (_portrait = MakePortrait(96));

    /// <summary>Angled lower console, stretched across the bottom of the screen.</summary>
    public static Sprite Console =>
        _console != null ? _console : (_console = MakeConsole(512, 256));

    public static Sprite PillarLeft =>
        _pillarLeft != null ? _pillarLeft : (_pillarLeft = MakePillar(256, 512, false));

    public static Sprite PillarRight =>
        _pillarRight != null ? _pillarRight : (_pillarRight = MakePillar(256, 512, true));

    /// <summary>Decorative segmented status strip (original, no text).</summary>
    public static Sprite Segments =>
        _segments != null ? _segments : (_segments = MakeSegments(256, 24));

    public static Sprite WeaponIcon(int index) =>
        WeaponIcons[Mathf.Clamp(index, 0, WeaponIcons.Length - 1)] ??
        (WeaponIcons[Mathf.Clamp(index, 0, WeaponIcons.Length - 1)] = MakeWeaponIcon(48, Mathf.Clamp(index, 0, 2)));

    // ---------------------------------------------------------------- builders

    static Texture2D NewTex(int w, int h, out Color32[] px)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        px = new Color32[w * h];
        return tex;
    }

    static Sprite Finish(Texture2D tex, Color32[] px)
    {
        tex.SetPixels32(px);
        tex.Apply(false, false);
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite MakeSolid(int w, int h, Color color)
    {
        var tex = NewTex(w, h, out var px);
        for (int i = 0; i < px.Length; i++) px[i] = color;
        return Finish(tex, px);
    }

    static Sprite MakePanel(int w, int h, Color fill, Color edge)
    {
        var tex = NewTex(w, h, out var px);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool border = x < 3 || y < 3 || x >= w - 3 || y >= h - 3;
                bool innerLine = x == 6 || y == 6 || x == w - 7 || y == h - 7;
                Color c = border ? edge : fill;

                // Sparse rivets at the corners give the panel an industrial read.
                bool rivet = (x - 12) * (x - 12) + (y - 12) * (y - 12) < 6 ||
                             (x - (w - 13)) * (x - (w - 13)) + (y - 12) * (y - 12) < 6 ||
                             (x - 12) * (x - 12) + (y - (h - 13)) * (y - (h - 13)) < 6 ||
                             (x - (w - 13)) * (x - (w - 13)) + (y - (h - 13)) * (y - (h - 13)) < 6;
                if (rivet) c = Color.Lerp(c, edge, 0.85f);
                else if (innerLine) c = Color.Lerp(c, edge, 0.25f);

                px[y * w + x] = c;
            }
        }
        return Finish(tex, px);
    }

    static Sprite MakeCross(int size)
    {
        var tex = NewTex(size, size, out var px);
        int mid = size / 2;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool horizontal = y >= mid - 1 && y <= mid && x >= 6 && x <= size - 7 && (x < mid - 6 || x > mid + 6);
                bool vertical = x >= mid - 1 && x <= mid && y >= 6 && y <= size - 7 && (y < mid - 6 || y > mid + 6);
                bool dot = Mathf.Abs(x - mid) <= 1 && Mathf.Abs(y - mid) <= 1;
                px[y * size + x] = (horizontal || vertical || dot)
                    ? new Color(0.85f, 1f, 0.9f, 0.95f)
                    : new Color(0f, 0f, 0f, 0f);
            }
        }
        return Finish(tex, px);
    }

    static Sprite MakeBracket(int size, int thickness, Color color)
    {
        var tex = NewTex(size, size, out var px);
        int arm = Mathf.Max(6, size / 4);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool top = y < thickness && (x < arm || x >= size - arm);
                bool bottom = y >= size - thickness && (x < arm || x >= size - arm);
                bool left = x < thickness && (y < arm || y >= size - arm);
                bool right = x >= size - thickness && (y < arm || y >= size - arm);
                px[y * size + x] = top || bottom || left || right ? color : new Color(0f, 0f, 0f, 0f);
            }
        }
        return Finish(tex, px);
    }

    static Sprite MakeVignette(int size)
    {
        var tex = NewTex(size, size, out var px);
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((d - 0.55f) / 0.45f);
                a = a * a * 0.85f;
                px[y * size + x] = new Color(1f, 0.12f, 0.10f, a);
            }
        }
        return Finish(tex, px);
    }

    /// <summary>Lower-centre portrait: an original top-down SKIFF schematic, never text.</summary>
    static Sprite MakePortrait(int size)
    {
        var tex = NewTex(size, size, out var px);
        Color ink = new Color(0.52f, 0.86f, 1f, 0.95f);
        Color faint = new Color(0.20f, 0.30f, 0.36f, 0.9f);
        Color bg = new Color(0.05f, 0.07f, 0.09f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - size * 0.5f) / (size * 0.5f);
                float ny = (y - size * 0.5f) / (size * 0.5f);

                // Hull: a tapered diamond, drawn with simple normalised inequalities.
                bool hull = Mathf.Abs(nx) <= 0.30f * (1f - Mathf.Clamp01(ny * 0.75f + 0.35f)) &&
                            ny >= -0.75f && ny <= 0.72f;
                bool nose = Mathf.Abs(nx) <= 0.10f && ny >= 0.45f && ny <= 0.86f;
                bool wings = Mathf.Abs(nx) >= 0.18f && Mathf.Abs(nx) <= 0.52f &&
                             ny >= -0.10f && ny <= 0.28f;
                bool engine = Mathf.Abs(nx) <= 0.16f && ny >= -0.82f && ny <= -0.58f;

                Color c = bg;
                if (hull || nose || wings) c = faint;
                if (hull && Mathf.Abs(nx) <= 0.05f) c = ink;
                if (nose || engine) c = ink;
                if ((x < 2 || x >= size - 2 || y < 2 || y >= size - 2)) c = new Color(0.35f, 0.42f, 0.48f, 1f);

                px[y * size + x] = c;
            }
        }
        return Finish(tex, px);
    }

    /// <summary>Angled console: full width at the bottom, narrower at the top, with trim.</summary>
    static Sprite MakeConsole(int w, int h)
    {
        var tex = NewTex(w, h, out var px);
        Color fill = new Color(0.07f, 0.09f, 0.12f, 1f);
        Color panel = new Color(0.10f, 0.13f, 0.17f, 1f);
        Color trim = new Color(0.40f, 0.47f, 0.55f, 1f);
        Color seam = new Color(0.16f, 0.20f, 0.25f, 1f);

        float straightTop = h * 0.55f;
        float topInset = w * 0.215f;

        for (int y = 0; y < h; y++)
        {
            float inset = y <= straightTop ? 0f : Mathf.Lerp(0f, topInset, (y - straightTop) / (h - straightTop));
            int x0 = Mathf.RoundToInt(inset);
            int x1 = w - 1 - x0;

            for (int x = 0; x < w; x++)
            {
                if (x < x0 || x > x1)
                {
                    px[y * w + x] = new Color(0f, 0f, 0f, 0f);
                    continue;
                }

                bool border = x - x0 < 6 || x1 - x < 6 || y < 6 || y > h - 7 ||
                              (y > straightTop && (x - x0 < 10 + (y - straightTop) * 0.06f));
                bool rib = (x % 128) < 2 || (y % 64) < 2;

                Color c = border ? trim : rib ? seam : ((x / 64 + y / 64) % 2 == 0 ? fill : panel);
                if (!border && (x % 128) > 118 && (x % 128) < 126) c = seam;

                px[y * w + x] = c;
            }
        }
        return Finish(tex, px);
    }

    static Sprite MakePillar(int w, int h, bool right)
    {
        var tex = NewTex(w, h, out var px);
        Color fill = new Color(0.08f, 0.10f, 0.13f, 1f);
        Color panel = new Color(0.12f, 0.15f, 0.19f, 1f);
        Color trim = new Color(0.38f, 0.45f, 0.53f, 1f);

        for (int y = 0; y < h; y++)
        {
            float t = 1f - (float)y / (h - 1);
            // Inner edge leans in toward the top, framing the windscreen.
            float inner = Mathf.Lerp(w * 0.80f, w * 0.58f, t);

            for (int x = 0; x < w; x++)
            {
                float along = right ? (w - 1 - x) : x;
                if (along > inner)
                {
                    px[y * w + x] = new Color(0f, 0f, 0f, 0f);
                    continue;
                }

                bool border = along > inner - 7 || x < 4 || x > w - 5;
                bool rib = (y % 96) < 2;
                px[y * w + x] = border ? trim : rib ? new Color(0.18f, 0.22f, 0.27f) :
                                 ((x / 48 + y / 96) % 2 == 0 ? fill : panel);
            }
        }
        return Finish(tex, px);
    }

    static Sprite MakeSegments(int w, int h)
    {
        var tex = NewTex(w, h, out var px);
        Color dark = new Color(0.05f, 0.06f, 0.08f, 0.95f);
        Color off = new Color(0.35f, 0.16f, 0.06f, 1f);
        Color hot = new Color(1f, 0.55f, 0.12f, 1f);

        int segments = 10;
        int gap = 3;
        int segW = (w - gap * (segments + 1)) / segments;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int index = x / (segW + gap);
                int local = x - index * (segW + gap) - gap;
                bool inside = local >= 0 && local < segW && y > 3 && y < h - 4;
                if (!inside)
                {
                    px[y * w + x] = dark;
                    continue;
                }
                bool lit = index < 6; // reads as a partially filled status bar
                px[y * w + x] = lit ? hot : off;
            }
        }
        return Finish(tex, px);
    }

    static Sprite MakeWeaponIcon(int size, int kind)
    {
        var tex = NewTex(size, size, out var px);
        Color c = kind == 0 ? new Color(0.55f, 1f, 0.65f) :
                  kind == 1 ? new Color(0.55f, 0.85f, 1f) :
                              new Color(1f, 0.68f, 0.25f);
        Color empty = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - size * 0.5f) / (size * 0.5f);
                float ny = (y - size * 0.5f) / (size * 0.5f);
                bool mark;
                switch (kind)
                {
                    case 0: // pulse: a single compact chevron
                        mark = Mathf.Abs(nx) <= 0.10f && Mathf.Abs(ny) <= 0.42f;
                        break;
                    case 1: // spread: three diverging strokes
                        mark = (Mathf.Abs(nx + ny * 0.55f) <= 0.07f || Mathf.Abs(nx - ny * 0.55f) <= 0.07f ||
                                Mathf.Abs(nx) <= 0.06f) && Mathf.Abs(ny) <= 0.45f;
                        break;
                    default: // heavy: a solid slug with a tip
                        mark = Mathf.Abs(nx) <= 0.16f && Mathf.Abs(ny) <= 0.34f ||
                               Mathf.Abs(nx) <= 0.07f && ny >= 0.34f && ny <= 0.52f;
                        break;
                }
                px[y * size + x] = mark ? c : empty;
            }
        }
        return Finish(tex, px);
    }
}
