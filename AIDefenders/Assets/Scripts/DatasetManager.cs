using UnityEngine;
using System.IO;
using System.Collections;

public class DatasetManager : MonoBehaviour
{
    [Header("Capture Settings")]
    public Camera datasetCamera;            //촬영할 카메라
    public RenderTexture renderTexture;     //출력 영상 저장 텍스쳐

    [Header("Objects")]
    public Transform target;                //촬영 대상
    public Animator targetAnimator;         //촬영 대상 애니메이터
    public string[] animationStates;        //대상의 애니메이션 리스트
    public Light directionalLight;          //태양광


    [Header("Dataset")]
    public int sampleCount = 100;

    public int classID = 0;             //YOLO 
    public string label = "enemy";      //해당 ID에 지어줄 이름

    Texture2D screenTexture;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!datasetCamera) return;
        if (!target) return;

        RenderSettings.fogMode = FogMode.Exponential;

        screenTexture = new Texture2D(
            renderTexture.width,
            renderTexture.height,
            TextureFormat.RGB24,
            false
        );

        GenerateDataset();
    }

    //데이터셋 생성
    IEnumerator GenerateDataset()
    {
        CreateDirectories();

        for (int i = 0; i < sampleCount; i++)
        {
            RandomizeScene();

            RandomizeAnimation();

            yield return null;

            CaptureAndSave(i);
        }

        Debug.Log("Dataset generation complete!");
    }


    // /dataset/(images, labels)/(학습, 검증, 테스트) 별로 나눈 폴더 생성
    void CreateDirectories()
    {
        string[] splits = { "train", "val", "test" };

        foreach (string split in splits)
        {
            string imagePath =
                Application.dataPath +
                "/Dataset/images/" +
                split;

            string labelPath =
                Application.dataPath +
                "/Dataset/labels/" +
                split;

            if (!Directory.Exists(imagePath))
                Directory.CreateDirectory(imagePath);

            if (!Directory.Exists(labelPath))
                Directory.CreateDirectory(labelPath);
        }
    }

    /*
    //레이블별로 나눈 폴더 생성
    void CreateDirectories()
    {
        string folderPath =
            Application.dataPath +
            "/Dataset/" +
            label;

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
    }
    */


    //주변환경 무작위화
    void RandomizeScene()
    {
        //대상
        target.position = new Vector3(
            Random.Range(-5f, 5f),
            1f,
            Random.Range(-5f, 5f)
        );

        //카메라
        datasetCamera.transform.position = new Vector3(
            Random.Range(-8f, 8f),
            Random.Range(4f, 10f),
            Random.Range(-8f, 8f)
        );

        datasetCamera.transform.LookAt(target);

        //자연광
        float sunPitch = Random.Range(15f, 80f);
        float sunYaw = Random.Range(0f, 360f);

        directionalLight.transform.rotation = Quaternion.Euler(sunPitch, sunYaw, 0f);
        directionalLight.intensity = Random.Range(1.2f, 2f);
        directionalLight.color = new Color(
            Random.Range(0.8f, 1f),
            Random.Range(0.8f, 1f),
            Random.Range(0.8f, 1f)
        );

        //안개
        RenderSettings.fog =
        Random.value > 0.5f;

        RenderSettings.fogColor = new Color(
            Random.Range(0.7f, 1f),
            Random.Range(0.7f, 1f),
            Random.Range(0.7f, 1f)
        );

        RenderSettings.fogDensity =
            Random.Range(0.002f, 0.02f);
    }

    //캡쳐 및 저장
    void CaptureAndSave(int index)
    {
        string split = GetDatasetSplit();

        RenderTexture currentRT = RenderTexture.active;

        RenderTexture.active = renderTexture;

        datasetCamera.Render();

        screenTexture.ReadPixels(
            new Rect(0, 0, renderTexture.width, renderTexture.height),
            0,
            0
        );

        screenTexture.Apply();

        RenderTexture.active = currentRT;

        byte[] bytes = screenTexture.EncodeToJPG(90);

        //예: 레이블이 tank이면 /Dataset/images/train/tank_0.jpg
        string imageName = label + "_" + index;
        string imagePath =      
        Application.dataPath +
        "/Dataset/images/" +
        split +
        "/" +
        imageName +
        ".jpg";

        File.WriteAllBytes(imagePath, bytes);

        SaveLabel(imageName, split);
    }

    //데이터 나누기
    string GetDatasetSplit()
    {
        float rand = Random.value;

        if (rand < 0.7f)
            return "train";

        if (rand < 0.9f)
            return "val";

        return "test";
    }

    //레이블 데이터 생성
    void SaveLabel(string imageName, string split)
    {
        string yoloLabel = GenerateYOLOBBox();

        if (yoloLabel == null)
            return;

        string labelPath =
            Application.dataPath +
            "/Dataset/labels/" +
            split +
            "/" +
            imageName +
            ".txt";

        File.WriteAllText(labelPath, yoloLabel);
    }

    //레이블 데이터 생성 시 필요한 BBox 계산
    string GenerateYOLOBBox()
    {
        Target_Base targetbase = target.GetComponent<Target_Base>();
        Renderer renderer = targetbase.getRenderer();

        Bounds bounds = renderer.bounds;

        //대상의 렌더 상자의 8개의 꼭짓점. 
        Vector3[] corners = new Vector3[8];
        corners[0] = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
        corners[1] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        corners[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        corners[3] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
        corners[4] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
        corners[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
        corners[6] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
        corners[7] = new Vector3(bounds.max.x, bounds.max.y, bounds.max.z);

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        foreach (Vector3 corner in corners)
        {
            Vector3 screenPoint =
                datasetCamera.WorldToScreenPoint(corner);

            if (screenPoint.z < 0)
                return null;

            min.x = Mathf.Min(min.x, screenPoint.x);
            min.y = Mathf.Min(min.y, screenPoint.y);

            max.x = Mathf.Max(max.x, screenPoint.x);
            max.y = Mathf.Max(max.y, screenPoint.y);
        }

        float width = renderTexture.width;
        float height = renderTexture.height;

        float xCenter = ((min.x + max.x) / 2f) / width;
        float yCenter = ((min.y + max.y) / 2f) / height;
        yCenter = 1f - yCenter;         //YOLO에 맞게 뒤집기 필요

        float boxWidth = (max.x - min.x) / width;
        float boxHeight = (max.y - min.y) / height;

        return
        classID + " " +
        xCenter + " " +
        yCenter + " " +
        boxWidth + " " +
        boxHeight;
    }

    //애니메이션 무작위화
    private void RandomizeAnimation()
    {
        if (targetAnimator == null)
            return;
        if (animationStates.Length == 0)
            return;

        string state = animationStates[Random.Range(0, animationStates.Length)];

        targetAnimator.Play(state, 0, Random.value);
    }

        // Update is called once per frame
        void Update()
    {
        
    }
}