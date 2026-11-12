using UnityEngine;
using UnityEngine.Events;
namespace AIDefenders.Assets {
// Local visual animation; aim, damage and projectile movement are owned by gameplay.
public sealed class PlasmaLevel3FireAnimation : MonoBehaviour {
    public Transform emitterPivot, chargeOrb;
    public Renderer[] glowRenderers;
    public bool autoFire;
    [Tooltip("Check during Play mode to trigger one shot.")] public bool fireOnce;
    [Min(.05f)] public float chargeSeconds=.30f;
    [Min(.01f)] public float flashSeconds=.05f;
    [Min(.01f)] public float recoverySeconds=.40f;
    [Min(.1f)] public float fireInterval=1.25f;
    [Range(0f,.3f)] public float recoilDistance=.10f;
    public UnityEvent onShot=new UnityEvent();
    public bool IsBusy { get; private set; }
    Vector3 home,orbScale; bool orbActive,ready,shotSent;
    float elapsed,nextAuto;
    MaterialPropertyBlock[] saved,working;
    Color[] emission,baseColors;
    static readonly int Emission=Shader.PropertyToID("_EmissionColor");
    static readonly int BaseColor=Shader.PropertyToID("_BaseColor"),ColorId=Shader.PropertyToID("_Color");
    void Awake() { Initialize(); }
    bool Initialize() {
        if(ready) return true;
        if(emitterPivot==null || chargeOrb==null) return false;
        home=emitterPivot.localPosition;orbScale=chargeOrb.localScale;orbActive=chargeOrb.gameObject.activeSelf;
        if(glowRenderers==null) glowRenderers=new Renderer[0];
        saved=new MaterialPropertyBlock[glowRenderers.Length];working=new MaterialPropertyBlock[glowRenderers.Length];
        emission=new Color[glowRenderers.Length];baseColors=new Color[glowRenderers.Length];
        for(int i=0;i<glowRenderers.Length;i++) {
            saved[i]=new MaterialPropertyBlock();working[i]=new MaterialPropertyBlock();
            if(glowRenderers[i]==null) continue;
            glowRenderers[i].GetPropertyBlock(saved[i]);
            Material mat=glowRenderers[i].sharedMaterial;
            emission[i]=mat!=null && mat.HasProperty(Emission) ? mat.GetColor(Emission) : Color.black;
            baseColors[i]=mat!=null ? mat.color : Color.white;
        }
        ready=true;return true;
    }
    // True means a new charge cycle was accepted. The actual shot happens at On Shot.
    public bool PlayFire() {
        if(!isActiveAndEnabled || !Initialize() || IsBusy) return false;
        IsBusy=true;elapsed=0;shotSent=false;chargeOrb.gameObject.SetActive(true);
        chargeOrb.localScale=orbScale*.45f;return true;
    }
    void Glow(float gain,bool flash) {
        for(int i=0;i<glowRenderers.Length;i++) {
            Renderer r=glowRenderers[i];if(r==null) continue;
            r.GetPropertyBlock(working[i]);
            working[i].SetColor(Emission,flash ? Color.white*5f : emission[i]*gain);
            working[i].SetColor(BaseColor,flash ? Color.white : baseColors[i]);
            working[i].SetColor(ColorId,flash ? Color.white : baseColors[i]);
            r.SetPropertyBlock(working[i]);
        }
    }
    void Update() {
        if(fireOnce) { fireOnce=false;PlayFire(); }
        if(autoFire && !IsBusy && Time.time>=nextAuto) {
            if(PlayFire()) nextAuto=Time.time+Mathf.Max(.1f,fireInterval);
        }
        if(!IsBusy) return;
        elapsed+=Time.deltaTime;float charge=Mathf.Max(.05f,chargeSeconds),flash=Mathf.Max(.01f,flashSeconds),recovery=Mathf.Max(.01f,recoverySeconds);
        if(elapsed<charge) {
            float t=Mathf.Clamp01(elapsed/charge);chargeOrb.localScale=orbScale*Mathf.Lerp(.45f,1.2f,t*t*(3-2*t));
            Glow(1+2*t,false);return;
        }
        if(!shotSent) {
            shotSent=true;onShot.Invoke();
            // A listener may disable or destroy this object.
            if(this==null || !isActiveAndEnabled || !IsBusy) return;
        }
        float after=elapsed-charge;
        if(after<flash) {
            chargeOrb.gameObject.SetActive(true);chargeOrb.localScale=orbScale*1.5f;
            emitterPivot.localPosition=home+Vector3.back*recoilDistance;
            Glow(5f,true);return;
        }
        float rtime=Mathf.Clamp01((after-flash)/recovery);
        emitterPivot.localPosition=home+Vector3.back*(recoilDistance*(1-rtime)*(1-rtime));
        float regrow=Mathf.Clamp01((rtime-.35f)/.65f);
        chargeOrb.gameObject.SetActive(regrow>0);chargeOrb.localScale=orbScale*regrow;
        Glow(Mathf.Lerp(2.5f,1,rtime),false);
        if(rtime>=1) { Restore();nextAuto=Mathf.Max(nextAuto,Time.time+.05f); }
    }
    public void CancelFire() { Restore(); }
    void Restore() {
        IsBusy=false;if(!ready) return;
        if(emitterPivot!=null) emitterPivot.localPosition=home;
        if(chargeOrb!=null) { chargeOrb.localScale=orbScale;chargeOrb.gameObject.SetActive(orbActive); }
        for(int i=0;i<glowRenderers.Length;i++) if(glowRenderers[i]!=null) glowRenderers[i].SetPropertyBlock(saved[i]);
    }
    void OnDisable() { Restore(); }
}
}
