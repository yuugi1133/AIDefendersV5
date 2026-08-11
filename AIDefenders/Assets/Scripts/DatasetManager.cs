using UnityEngine;
using System.IO;
using System.Collections;

public class DatasetManager : MonoBehaviour
{
    [Header("Capture Settings")]
    public Camera datasetCamera;            //�Կ��� ī�޶�
    public RenderTexture renderTexture;     //���? ���� ���� �ؽ���

    [Header("Objects")]
    public Transform target;                //�Կ� ���?
    public Animator targetAnimator;         //�Կ� ���? �ִϸ�����
    public string[] animationStates;        //�����? �ִϸ��̼� ����Ʈ
    public Light directionalLight;          //�¾籤


    [Header("Dataset")]
    public int sampleCount = 100;

    public int classID = 0;             //YOLO 
    public string label = "enemy";      //�ش� ID�� ������ �̸�

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

        StartCoroutine(GenerateDataset());
    }

    //�����ͼ� ����
    IEnumerator GenerateDataset()
    {
        CreateDirectories();

        for (int i = 0; i < sampleCount; i++)
        {
            RandomizeScene();

            RandomizeAnimation();

            yield return null;
            yield return new WaitForEndOfFrame();

            CaptureAndSave(i);
        }

        Debug.Log("Dataset generation complete!");
    }


    // /dataset/(images, labels)/(�н�, ����, �׽�Ʈ) ���� ���� ���� ����
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
    //���̺����� ���� ���� ����
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


    //�ֺ�ȯ�� ������ȭ
    void RandomizeScene()
    {
        //���?
        target.position = new Vector3(
            Random.Range(-5f, 5f),
            1f,
            Random.Range(-5f, 5f)
        );

        //ī�޶�
        datasetCamera.transform.position = new Vector3(
            Random.Range(-8f, 8f),
            Random.Range(4f, 10f),
            Random.Range(-8f, 8f)
        );

        datasetCamera.transform.LookAt(target);

        //�ڿ���
        float sunPitch = Random.Range(15f, 80f);
        float sunYaw = Random.Range(0f, 360f);

        directionalLight.transform.rotation = Quaternion.Euler(sunPitch, sunYaw, 0f);
        directionalLight.intensity = Random.Range(1.2f, 2f);
        directionalLight.color = new Color(
            Random.Range(0.8f, 1f),
            Random.Range(0.8f, 1f),
            Random.Range(0.8f, 1f)
        );

        //�Ȱ�
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

    //ĸ�� �� ����
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

        //��: ���̺��� tank�̸� /Dataset/images/train/tank_0.jpg
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

    //������ ������
    string GetDatasetSplit()
    {
        float rand = Random.value;

        if (rand < 0.7f)
            return "train";

        if (rand < 0.9f)
            return "val";

        return "test";
    }

    //���̺� ������ ����
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

    //���̺� ������ ���� �� �ʿ��� BBox ���?
    string GenerateYOLOBBox()
    {
        Target_Base targetbase = target.GetComponent<Target_Base>();
        Renderer renderer = targetbase.getRenderer();

        Bounds bounds = renderer.bounds;

        //�����? ���� ������ 8���� ������. 
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
        yCenter = 1f - yCenter;         //YOLO�� �°� ������ �ʿ�

        float boxWidth = (max.x - min.x) / width;
        float boxHeight = (max.y - min.y) / height;

        return
        classID + " " +
        xCenter + " " +
        yCenter + " " +
        boxWidth + " " +
        boxHeight;
    }

    //�ִϸ��̼� ������ȭ
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