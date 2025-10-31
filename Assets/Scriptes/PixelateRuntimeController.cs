using UnityEngine;
using UnityEngine.UI;

public class PixelateRuntimeController : MonoBehaviour
{
    [Header("UI References")]
    public Slider pixelSlider;        // ピクセル粗さ（仮想幅）
    public Slider colorStepsSlider;   // 色段階
    public Slider halftoneStrength;   // ハーフトーン強度 0..1
    public Toggle enableToggle;       // エフェクトON/OFF
    public Toggle halftoneToggle;     // ハーフトーンON/OFF

    [Header("Virtual Resolution")]
    public int minVirtualWidth = 120;   // 最小の“疑似横ドット数”
    public int maxVirtualWidth = 640;   // 最大の“疑似横ドット数”
    public int defaultVirtualWidth = 320;

    public int defaultColorSteps = 8;

    // Global property ids
    static readonly int ID_Mode             = Shader.PropertyToID("_Pixelate_Mode");
    static readonly int ID_PixelWidth       = Shader.PropertyToID("_Pixelate_PixelWidth");
    static readonly int ID_PixelHeight      = Shader.PropertyToID("_Pixelate_PixelHeight");
    static readonly int ID_ColorSteps       = Shader.PropertyToID("_Pixelate_ColorSteps");
    static readonly int ID_HalftoneScale    = Shader.PropertyToID("_Pixelate_HalftoneScale");
    static readonly int ID_HalftoneStrength = Shader.PropertyToID("_Pixelate_HalftoneStrength");

    void Start()
    {
        // 初期値設定
        if (pixelSlider)
        {
            pixelSlider.minValue = minVirtualWidth;
            pixelSlider.maxValue = maxVirtualWidth;
            pixelSlider.value    = defaultVirtualWidth;
            pixelSlider.onValueChanged.AddListener(_ => Apply());
        }

        if (colorStepsSlider)
        {
            colorStepsSlider.minValue = 2;
            colorStepsSlider.maxValue = 32;
            colorStepsSlider.wholeNumbers = true;
            colorStepsSlider.value = defaultColorSteps;
            colorStepsSlider.onValueChanged.AddListener(_ => Apply());
        }

        if (halftoneStrength)
        {
            halftoneStrength.minValue = 0f;
            halftoneStrength.maxValue = 1f;
            halftoneStrength.value = 0.35f;
            halftoneStrength.onValueChanged.AddListener(_ => Apply());
        }

        if (enableToggle)   enableToggle.onValueChanged.AddListener(_ => Apply());
        if (halftoneToggle) halftoneToggle.onValueChanged.AddListener(_ => Apply());

        // ハーフトーン密度の既定（1.0 = ほどよい）
        Shader.SetGlobalFloat(ID_HalftoneScale, 1.0f);

        Apply(); // 初期反映
    }

    void OnValidate()
    {
        if (Application.isPlaying) Apply();
    }

    void Apply()
    {
        // 画面アスペクトから縦ドット数を合わせて“正方ピクセル”風に
        float virtualW = pixelSlider ? pixelSlider.value : defaultVirtualWidth;
        float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        float virtualH = Mathf.Round(virtualW / Mathf.Max(0.1f, aspect));

        // Mode: 0=Off, 1=Pixel, 2=Pixel+Halftone
        int mode = 0;
        if (enableToggle == null || enableToggle.isOn)
            mode = (halftoneToggle != null && halftoneToggle.isOn) ? 2 : 1;

        Shader.SetGlobalFloat(ID_Mode, mode);
        Shader.SetGlobalFloat(ID_PixelWidth,  virtualW);
        Shader.SetGlobalFloat(ID_PixelHeight, virtualH);

        float steps = colorStepsSlider ? colorStepsSlider.value : defaultColorSteps;
        Shader.SetGlobalFloat(ID_ColorSteps, steps);

        float hStr = halftoneStrength ? halftoneStrength.value : 0.35f;
        Shader.SetGlobalFloat(ID_HalftoneStrength, hStr);
        // ハーフトーン密度は必要なら別 Slider を追加して ID_HalftoneScale を触ってください
    }
}
