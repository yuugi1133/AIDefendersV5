using UnityEngine;

public class CCTVRadar : MonoBehaviour
{
    public float range = 45f;
    public float size = 168f;
    public float margin = 16f;
    public float blipSize = 7f;

    public Color backgroundColor = new Color(0.05f, 0.08f, 0.08f, 0.72f);
    public Color borderColor = new Color(0.55f, 0.85f, 0.7f, 0.9f);
    public Color playerColor = new Color(1f, 1f, 1f, 1f);
    public Color unknownColor = new Color(0.65f, 0.65f, 0.65f, 1f);
    public Color enemyColor = new Color(0.95f, 0.18f, 0.14f, 1f);
    public Color friendlyColor = new Color(0.2f, 0.45f, 1f, 1f);

    WaveDirector waveDirector;
    CCTVController cctv;

    public CCTVController Cctv
    {
        get
        {
            if (cctv == null)
            {
                cctv = GetComponent<CCTVController>();
                if (cctv == null)
                    cctv = FindFirstObjectByType<CCTVController>();
            }

            return cctv;
        }
    }

    public bool IsVisible
    {
        get
        {
            if (waveDirector == null)
                waveDirector = FindFirstObjectByType<WaveDirector>();
            return waveDirector != null && waveDirector.Phase == WavePhase.Combat;
        }
    }

    void Start()
    {
        waveDirector = FindFirstObjectByType<WaveDirector>();
        cctv = GetComponent<CCTVController>();
        if (cctv == null)
            cctv = FindFirstObjectByType<CCTVController>();
    }
}
