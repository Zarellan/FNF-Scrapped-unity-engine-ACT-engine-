using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class PostProcessUtil : MonoBehaviour
{
    public static PostProcessUtil instance;
    PostProcessVolume postProcessVolume;

    private Vignette vignette; public Vignette VigenetteType{get{ return vignette; }set{ vignette = value; }} //if you want to customize it

    private ChromaticAberration chromatic;

    private ColorGrading colorGrading;

    Tween vigenetteIntense, chromaticIntense, saturationTween;
    void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        postProcessVolume = GameObject.FindWithTag("PostProcess").GetComponent<PostProcessVolume>();
        postProcessVolume.profile.TryGetSettings(out vignette);
        postProcessVolume.profile.TryGetSettings(out chromatic);
        postProcessVolume.profile.TryGetSettings(out colorGrading);
    }
    public void VignetteEffect(float intensity, float tweenTime = 0,Ease easeType = Ease.Linear)
    {
        if (vignette == null)
        {
            Debug.LogWarning("vignette isn't enabled");
            return;
        }
        if (vigenetteIntense.isAlive)
            vigenetteIntense.Stop();
            
        if (tweenTime == 0)
            vignette.intensity.value = intensity;
        else
            vigenetteIntense = Tween.Custom(vignette.intensity.value, intensity, duration: tweenTime, onValueChange: newVal =>
                {
                    vignette.intensity.value = newVal;
                }, ease: easeType,useUnscaledTime:true);

    }
    
    public void ChromaticEffect(float intensity)
    {
        if (chromatic == null)
        {
            Debug.LogWarning("chromatic isn't enabled");
            return;
        }
        chromatic.intensity.value = intensity;
    }

    public void ColorGradingToneEffect(float saturation, float tweenTime = 0, Ease easeType = Ease.Linear)
    {
        if (colorGrading == null)
        {
            Debug.LogWarning("colorGrading isn't enabled");
            return;
        }

        if (saturationTween.isAlive)
            saturationTween.Stop();
            
        if (tweenTime == 0)
            colorGrading.saturation.value = saturation;
        else
            saturationTween = Tween.Custom(vignette.intensity.value, saturation, duration: tweenTime, onValueChange: newVal =>
                {
                    colorGrading.saturation.value = newVal;
                }, ease: easeType,useUnscaledTime:true);

    }
    
}
