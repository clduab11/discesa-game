using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space cockpit HUD. The centre stays transparent so the 3D ship nose and
/// world render through; an original bezel frames it. Layout is locked:
///   top-left shields, top-right energy, bottom-left weapon, bottom-right ammo,
///   centre three-part reticle, lower-centre SKIFF portrait.
///
/// The HUD only reads <see cref="HudModel"/> snapshots; it never mutates game state.
/// Art comes from <see cref="HudArt"/> (original, procedural). No reference cockpit
/// images or branding are used.
/// </summary>
public class CockpitHud : MonoBehaviour
{
    public Canvas Canvas { get; private set; }

    RectTransform _root;
    Image _vignette;
    Image _shieldFill;
    Image _energyFill;
    Image _weaponIcon;
    Image _reticleCross;
    Image _reticlePrimary;
    Image _reticleSecondary;
    Image _portraitImage;

    Text _shieldText;
    Text _energyText;
    Text _weaponText;
    Text _slotText;
    Text _missileText;
    Text _costText;
    Text _objectiveText;
    Text _banner;
    Text _pickupText;

    readonly Color _shieldColor = new Color(0.35f, 0.92f, 1f);
    readonly Color _energyColor = new Color(1f, 0.75f, 0.30f);
    readonly Color _dangerColor = new Color(1f, 0.30f, 0.24f);

    float _pickupTimer;
    string _pickupMessage;
    int _lastShield = int.MinValue, _lastEnergy = int.MinValue, _lastMissiles = int.MinValue;
    int _lastWeapon = int.MinValue, _lastCore = int.MinValue;
    bool _lastWin, _lastLose;
    bool _built;

    public void Build()
    {
        if (_built) return;
        _built = true;

        Canvas = gameObject.GetComponent<Canvas>();
        if (Canvas == null) Canvas = gameObject.AddComponent<Canvas>();
        Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        Canvas.sortingOrder = 10;

        var scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        _root = NewRect(transform, "HudRoot", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        BuildVignette();
        BuildBezel();
        BuildResourcePanel(true);
        BuildResourcePanel(false);
        BuildWeaponPanel();
        BuildAmmoPanel();
        BuildReticle();
        BuildPortrait();
        BuildBanner();
    }

    // ---------------------------------------------------------------- build

    void BuildVignette()
    {
        _vignette = NewImage("DamageVignette", _root, HudArt.Vignette, new Color(1f, 1f, 1f, 0f));
        Stretch(_vignette.rectTransform);
        _vignette.raycastTarget = false;
    }

    void BuildBezel()
    {
        // Reference-style frame: a thick angled lower console and side pillars with a
        // fully transparent windscreen in the middle. All art is original (HudArt).
        var consoleImg = NewImage("Console", _root, HudArt.Console, Color.white);
        Anchor(consoleImg.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            Vector2.zero, new Vector2(0f, 250f));
        consoleImg.raycastTarget = false;

        var leftPillar = NewImage("PillarLeft", _root, HudArt.PillarLeft, Color.white);
        Anchor(leftPillar.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f),
            new Vector2(0f, 250f), new Vector2(110f, -250f));
        leftPillar.raycastTarget = false;

        var rightPillar = NewImage("PillarRight", _root, HudArt.PillarRight, Color.white);
        Anchor(rightPillar.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0f),
            new Vector2(0f, 250f), new Vector2(110f, -250f));
        rightPillar.raycastTarget = false;

        var top = NewImage("BezelTop", _root, HudArt.PanelDark, new Color(1f, 1f, 1f, 0.92f));
        Anchor(top.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(0f, 16f));
        top.raycastTarget = false;

        AddSegmentStrip(new Vector2(-300f, 184f));
        AddSegmentStrip(new Vector2(300f, 184f));
    }

    void AddSegmentStrip(Vector2 position)
    {
        var strip = NewImage("StatusSegments", _root, HudArt.Segments, Color.white);
        Anchor(strip.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            position, new Vector2(220f, 20f));
        strip.raycastTarget = false;
    }

    void BuildResourcePanel(bool shield)
    {
        string prefix = shield ? "Shield" : "Energy";
        var panel = NewImage(prefix + "Panel", _root, HudArt.Panel, Color.white);
        Anchor(panel.rectTransform,
            shield ? new Vector2(0f, 1f) : new Vector2(1f, 1f),
            shield ? new Vector2(0f, 1f) : new Vector2(1f, 1f),
            shield ? new Vector2(0f, 1f) : new Vector2(1f, 1f),
            new Vector2(shield ? 28f : -28f, -26f),
            new Vector2(336f, 96f));
        panel.raycastTarget = false;

        var label = NewText(prefix + "Label", panel.rectTransform, 18, new Color(0.72f, 0.82f, 0.90f), TextAnchor.MiddleLeft);
        Anchor(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(14f, -10f), new Vector2(210f, 24f));
        label.text = shield ? "SHIELDS" : "ENERGY";

        var value = NewText(prefix + "Value", panel.rectTransform, 24, Color.white, TextAnchor.MiddleRight);
        Anchor(value.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-14f, -8f), new Vector2(104f, 30f));
        value.text = "100";

        var barBg = NewImage(prefix + "BarBg", panel.rectTransform, HudArt.White, new Color(0.03f, 0.04f, 0.06f, 1f));
        Anchor(barBg.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 14f), new Vector2(-24f, 20f));
        barBg.raycastTarget = false;

        var fill = NewImage(prefix + "Fill", barBg.rectTransform, HudArt.White, shield ? _shieldColor : _energyColor);
        fill.rectTransform.anchorMin = new Vector2(0f, 0f);
        fill.rectTransform.anchorMax = new Vector2(1f, 1f);
        fill.rectTransform.offsetMin = new Vector2(2f, 2f);
        fill.rectTransform.offsetMax = new Vector2(-2f, -2f);
        fill.raycastTarget = false;

        if (shield)
        {
            _shieldFill = fill;
            _shieldText = value;
        }
        else
        {
            _energyFill = fill;
            _energyText = value;
        }
    }

    void BuildWeaponPanel()
    {
        var panel = NewImage("WeaponPanel", _root, HudArt.Panel, Color.white);
        Anchor(panel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(46f, 44f), new Vector2(300f, 84f));
        panel.raycastTarget = false;

        _weaponIcon = NewImage("WeaponIcon", panel.rectTransform, HudArt.WeaponIcon(0), Color.white);
        Anchor(_weaponIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(14f, 0f), new Vector2(52f, 52f));
        _weaponIcon.raycastTarget = false;

        _weaponText = NewText("WeaponName", panel.rectTransform, 26, new Color(0.70f, 1f, 0.78f), TextAnchor.MiddleLeft);
        Anchor(_weaponText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(78f, -12f), new Vector2(-78f, 32f));
        _weaponText.text = "PULSE";

        _slotText = NewText("WeaponSlot", panel.rectTransform, 15, new Color(0.62f, 0.72f, 0.80f), TextAnchor.MiddleLeft);
        Anchor(_slotText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f),
            new Vector2(78f, 12f), new Vector2(-78f, 24f));
        _slotText.text = "PRIMARY";
    }

    void BuildAmmoPanel()
    {
        var panel = NewImage("AmmoPanel", _root, HudArt.Panel, Color.white);
        Anchor(panel.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-46f, 44f), new Vector2(288f, 84f));
        panel.raycastTarget = false;

        _missileText = NewText("Missiles", panel.rectTransform, 24, new Color(1f, 0.74f, 0.34f), TextAnchor.MiddleRight);
        Anchor(_missileText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-14f, -12f), new Vector2(-28f, 30f));
        _missileText.text = "MISSILES 06";

        _costText = NewText("WeaponCost", panel.rectTransform, 16, new Color(0.62f, 0.72f, 0.80f), TextAnchor.MiddleRight);
        Anchor(_costText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-14f, 14f), new Vector2(-28f, 24f));
        _costText.text = "ENERGY 2";

        var objective = NewText("Objective", _root, 20, new Color(0.55f, 0.95f, 1f), TextAnchor.MiddleCenter);
        Anchor(objective.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -20f), new Vector2(420f, 30f));
        objective.text = "CORE 150";
        _objectiveText = objective;
    }

    void BuildReticle()
    {
        _reticleCross = NewImage("ReticleCross", _root, HudArt.Cross, Color.white);
        Anchor(_reticleCross.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(64f, 64f));
        _reticleCross.raycastTarget = false;

        _reticlePrimary = NewImage("ReticlePrimary", _root, HudArt.PrimaryBracket, Color.white);
        Anchor(_reticlePrimary.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(42f, 42f));
        _reticlePrimary.raycastTarget = false;

        _reticleSecondary = NewImage("ReticleSecondary", _root, HudArt.SecondaryBracket, Color.white);
        Anchor(_reticleSecondary.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(58f, 58f));
        _reticleSecondary.raycastTarget = false;
    }

    void BuildPortrait()
    {
        var backing = NewImage("PortraitBacking", _root, HudArt.Panel, new Color(1f, 1f, 1f, 0.95f));
        Anchor(backing.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 24f), new Vector2(132f, 116f));
        backing.raycastTarget = false;

        _portraitImage = NewImage("Portrait", backing.rectTransform, HudArt.Portrait, Color.white);
        Anchor(_portraitImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -4f), new Vector2(96f, 96f));
        _portraitImage.raycastTarget = false;

        var tag = NewText("PortraitTag", backing.rectTransform, 14, new Color(0.55f, 0.78f, 0.90f), TextAnchor.MiddleCenter);
        Anchor(tag.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, 4f), new Vector2(0f, 18f));
        tag.text = "SKIFF";

        _pickupText = NewText("PickupMessage", _root, 20, new Color(0.55f, 1f, 0.80f), TextAnchor.MiddleCenter);
        Anchor(_pickupText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 266f), new Vector2(640f, 28f));
        _pickupText.text = string.Empty;
    }

    void BuildBanner()
    {
        _banner = NewText("Banner", _root, 44, Color.white, TextAnchor.MiddleCenter);
        Anchor(_banner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 96f), new Vector2(900f, 180f));
        _banner.text = string.Empty;
    }

    // ---------------------------------------------------------------- render

    public void Render(HudModel m)
    {
        if (!_built) Build();

        float pulse = m.LowShield ? (0.25f + 0.20f * Mathf.PingPong(Time.time * 3f, 1f)) : 0f;
        float alpha = Mathf.Max(m.DamageFlash * 0.95f, pulse);
        _vignette.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));

        _shieldFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(m.ShieldFraction), 1f);
        _shieldFill.color = m.LowShield ? _dangerColor : _shieldColor;
        _energyFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(m.EnergyFraction), 1f);
        _energyFill.color = m.LowEnergy ? _dangerColor : _energyColor;

        int shieldInt = Mathf.RoundToInt(m.Shield);
        if (shieldInt != _lastShield) { _lastShield = shieldInt; _shieldText.text = m.ShieldText; }
        int energyInt = Mathf.RoundToInt(m.Energy);
        if (energyInt != _lastEnergy) { _lastEnergy = energyInt; _energyText.text = m.EnergyText; }
        if (m.Missiles != _lastMissiles) { _lastMissiles = m.Missiles; _missileText.text = m.MissileText; }
        if (m.WeaponIndex != _lastWeapon)
        {
            _lastWeapon = m.WeaponIndex;
            _weaponText.text = m.WeaponName;
            _slotText.text = m.WeaponSlotText;
            _costText.text = m.CostText;
            _weaponIcon.sprite = HudArt.WeaponIcon(m.WeaponIndex);
            Color iconTint = m.WeaponIndex == 0 ? new Color(0.60f, 1f, 0.72f) :
                             m.WeaponIndex == 1 ? new Color(0.60f, 0.88f, 1f) :
                                                  new Color(1f, 0.72f, 0.34f);
            _weaponIcon.color = iconTint;
            _weaponText.color = iconTint;
        }
        int coreInt = Mathf.CeilToInt(m.CoreHp);
        if (coreInt != _lastCore || m.IsWin != _lastWin || m.IsLose != _lastLose)
        {
            _lastCore = coreInt;
            _lastWin = m.IsWin;
            _lastLose = m.IsLose;
            _objectiveText.text = m.ObjectiveText;
            _objectiveText.color = m.IsWin ? new Color(0.45f, 1f, 0.65f) :
                                    m.IsLose ? _dangerColor :
                                    new Color(0.55f, 0.95f, 1f);
            _banner.text = m.BannerText;
            _banner.color = m.IsWin ? new Color(0.45f, 1f, 0.65f) : new Color(1f, 0.45f, 0.40f);
        }

        if (_pickupTimer > 0f)
        {
            _pickupTimer -= Time.unscaledDeltaTime;
            if (_pickupTimer <= 0f) _pickupText.text = string.Empty;
        }
    }

    public void ShowPickup(string message)
    {
        if (!_built) Build();
        _pickupMessage = message;
        _pickupText.text = message;
        _pickupTimer = 1.6f;
    }

    public string PickupMessage => _pickupMessage;

    // ---------------------------------------------------------------- ui helpers

    RectTransform NewRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
                          Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    Image NewImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.type = Image.Type.Simple;
        return img;
    }

    Text NewText(string name, Transform parent, int fontSize, Color color, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var txt = go.GetComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = anchor;
        txt.raycastTarget = false;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        return txt;
    }

    static void Anchor(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
                       Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
