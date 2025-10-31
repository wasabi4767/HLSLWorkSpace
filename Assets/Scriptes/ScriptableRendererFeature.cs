using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PixelateFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent injectionPoint = RenderPassEvent.AfterRendering;
        public Shader shader;
        [Range(0,2)] public int mode = 1;  // 0=Off,1=Pixel,2=Pixel+Halftone
        public float pixelWidth  = 320;
        public float pixelHeight = 180;
        public float colorSteps  = 8;
        [Range(0.5f, 2f)] public float halftoneScale = 1.0f;
        [Range(0f,1f)] public float halftoneStrength = 0.35f;
    }

    class PixelatePass : ScriptableRenderPass
    {
        private Material _mat;
        private RenderTargetIdentifier _source;
        private RenderTargetHandle _tmp;
        private Settings _settings;
        private ProfilingSampler _sampler = new ProfilingSampler("PixelateSwitch");

        // Global property names
        static readonly int ID_Mode             = Shader.PropertyToID("_Pixelate_Mode");
        static readonly int ID_PixelWidth       = Shader.PropertyToID("_Pixelate_PixelWidth");
        static readonly int ID_PixelHeight      = Shader.PropertyToID("_Pixelate_PixelHeight");
        static readonly int ID_ColorSteps       = Shader.PropertyToID("_Pixelate_ColorSteps");
        static readonly int ID_HalftoneScale    = Shader.PropertyToID("_Pixelate_HalftoneScale");
        static readonly int ID_HalftoneStrength = Shader.PropertyToID("_Pixelate_HalftoneStrength");

        public PixelatePass(Settings s)
        {
            _settings = s;
            _tmp.Init("_TempPixelateTex");
        }

        public void Setup(in RenderTargetIdentifier src, Material mat)
        {
            _source = src;
            _mat = mat;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_mat == null) return;

            // 1) デフォルト（アセット設定）
            int   mode   = _settings.mode;
            float pW     = Mathf.Max(8f, _settings.pixelWidth);
            float pH     = Mathf.Max(8f, _settings.pixelHeight);
            float steps  = Mathf.Max(1f, _settings.colorSteps);
            float hScale = _settings.halftoneScale;
            float hStr   = _settings.halftoneStrength;

            // 2) Global が来ていれば上書き
            //   - Mode は負数なら無視、0/1/2なら採用という運用にします
            float gMode = Shader.GetGlobalFloat(ID_Mode);
            if (gMode >= 0.0f) mode = Mathf.Clamp(Mathf.RoundToInt(gMode), 0, 2);

            float gPW = Shader.GetGlobalFloat(ID_PixelWidth);
            if (gPW > 0.0f) pW = Mathf.Max(8f, gPW);

            float gPH = Shader.GetGlobalFloat(ID_PixelHeight);
            if (gPH > 0.0f) pH = Mathf.Max(8f, gPH);

            float gSteps = Shader.GetGlobalFloat(ID_ColorSteps);
            if (gSteps > 0.0f) steps = Mathf.Max(1f, gSteps);

            float gHScale = Shader.GetGlobalFloat(ID_HalftoneScale);
            if (gHScale > 0.0f) hScale = gHScale;

            float gHStr = Shader.GetGlobalFloat(ID_HalftoneStrength);
            if (gHStr > 0.0f) hStr = Mathf.Clamp01(gHStr);

            // 3) マテリアルへ反映
            _mat.SetInt  ("_Mode", mode);
            _mat.SetFloat("_PixelWidth",  pW);
            _mat.SetFloat("_PixelHeight", pH);
            _mat.SetFloat("_ColorSteps",  steps);
            _mat.SetFloat("_HalftoneScale", hScale);
            _mat.SetFloat("_HalftoneStrength", hStr);

            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, _sampler))
            {
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                cmd.GetTemporaryRT(_tmp.id, desc, FilterMode.Bilinear);
                Blit(cmd, _source, _tmp.Identifier(), _mat, 0);
                Blit(cmd, _tmp.Identifier(), _source);
                cmd.ReleaseTemporaryRT(_tmp.id);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    public Settings settings = new Settings();
    private PixelatePass _pass;
    private Material _material;

    public override void Create()
    {
        if (settings.shader == null)
            settings.shader = Shader.Find("Hidden/PixelateSwitch");

        if (settings.shader != null && _material == null)
            _material = CoreUtils.CreateEngineMaterial(settings.shader);

        _pass = new PixelatePass(settings)
        {
            renderPassEvent = settings.injectionPoint
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_material == null) return;
        var source = renderer.cameraColorTarget;
        _pass.Setup(source, _material);
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_material);
    }
}

