using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CamScript : MonoBehaviour
{

    public bool isRecording;
    public bool isNormalized_p;

    public int anglesWidth, anglesHeight, anglesLength;

    public int distanceXOffSet;

    public float[] refCubeGaps;

    public GameObject obstacleParent;

    public GameObject anglesGroup;

    private Vector3 spawnPos = new Vector3(32, 0.8f, 50.5f);
    private float middleHeight = 1.5f;

    public GameObject[] targets;
    public Light[] lightings;
    private int count = 0;
    private Quaternion initialRotation;
    private Vector2 initialXY;
    float timer = 0.0f;
    private float initialZ_gap = 0f;
    private float captureTimer;
    private int fileCounter = 0;


    public string save_path_file_name = "C:/Users/Nic_C/Documents/MDP Image Recognition/UnityImagesToTrain2";
    private string googleDrivePath = "/content/drive/MyDrive/CZ3004_MDP_Group_29/UnityImagesToTrain";

    private Camera cam;

    public GameObject refCube;

    private GameObject[] camAnglesReferenceCubes;

    public int initialCube = 0;
    public int initialLight = 0;
    private int initialTarget = 0;

    public float[] lightingIntensities;

    public GameObject targetRef;
    public GameObject refSpawn;
    private GameObject currentTarget;
    private bool hasEnded = false;

    public float timerEach = 0.2f;

    void GenerateCameraAnglesPosition()
    {

        camAnglesReferenceCubes = new GameObject[anglesWidth * anglesHeight * anglesLength];

        Vector3 refSize = refCube.GetComponent<MeshFilter>().sharedMesh.bounds.size;

        // Width
        float startPointZ = refCube.transform.position.z - ((anglesWidth-1) * refCubeGaps[0])/2;
        // Height 
        float startPointY = refCube.transform.position.y - (anglesHeight / 2) * refCubeGaps[1];
        // Length
        float startPointX = refCube.transform.position.x + distanceXOffSet;


        for (int w = 0; w < anglesWidth; w++) {
            for (int h = 0; h < anglesHeight; h++) {
                for (int l = 0; l < anglesLength; l++) {
                    camAnglesReferenceCubes[w + anglesWidth * (h + anglesHeight * l)] = Instantiate(refCube, new Vector3(startPointX + l*refCubeGaps[2], 
                        startPointY + h * refCubeGaps[1], startPointZ + w * refCubeGaps[0]), Quaternion.identity);
                    camAnglesReferenceCubes[w + anglesWidth * (h + anglesHeight * l)].transform.parent = anglesGroup.transform;
                }
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        foreach (Light lighting in lightings)
        {
            lighting.intensity = lightingIntensities[initialLight];
        }
        currentTarget = Instantiate(targets[initialTarget], refSpawn.transform.position, refSpawn.transform.rotation);
        currentTarget.transform.parent = obstacleParent.transform;

        GenerateCameraAnglesPosition();
        captureTimer = timerEach;

        cam = gameObject.GetComponent<Camera>();
    }

    void CamCapture(GameObject target)
    {
        string fileName = "/Characters/" + target.name.Substring(0, target.name.Length-7) + "_" + fileCounter + ".jpeg";
        ScreenCapture.CaptureScreenshot(save_path_file_name + fileName);
        WriteBoundingBoxToCSV(target, googleDrivePath + fileName, isNormalized_p);
        fileCounter++;
    }

    void WriteBoundingBoxToCSV(GameObject targetObject, string fileName, bool isNormalized) { 
        int pointers = 4;

        float xMin, yMin, xMax, yMax;

        string finalString = "";

        Vector3[] boundaries = new Vector3[pointers];

        Vector3 screenPos;

        for (int i = 0; i < pointers; i++) {
            screenPos = cam.WorldToScreenPoint(targetObject.transform.GetChild(1).GetChild(i).position);
            boundaries[i] = screenPos;
        }
        StreamWriter writer = new StreamWriter(save_path_file_name + "/UnityImages_Metadata.csv", true);



        xMin = (int)boundaries[2].x;
        yMin = (int)(Screen.height - boundaries[0].y);
        xMax = (int)boundaries[3].x;
        yMax = (int)(Screen.height - boundaries[1].y);

        if (isNormalized)
        {
            xMin /= Screen.width;
            xMax /= Screen.width;
            yMin /= Screen.height;
            yMax /= Screen.height;
        }
        // ,fileName,label,xMin,yMin,,,xMax,yMax
        finalString = string.Format(",{0},{1},{2},{3},,,{4},{5}", fileName , targetObject.name.Substring(0, targetObject.name.Length - 7), xMin, yMin, xMax, yMax);


        Debug.Log(finalString);

        //return;
        writer.WriteLine(finalString);

        writer.Flush();
        writer.Close();
    }

    void WriteBoundingBoxToCSV(GameObject targetObject, bool isNormalized)
    {
        int pointers = 4;

        float xMin, yMin, xMax, yMax;

        string finalString = "";

        Vector3[] boundaries = new Vector3[pointers];

        Vector3 screenPos;

        for (int i = 0; i < pointers; i++)
        {
            screenPos = cam.WorldToScreenPoint(targetObject.transform.GetChild(1).GetChild(i).position);
            boundaries[i] = screenPos;
        }
        xMin = (int)boundaries[2].x;
        yMin = (int)(Screen.height - boundaries[0].y);
        xMax = (int)boundaries[3].x;
        yMax = (int)(Screen.height - boundaries[1].y);

        if (isNormalized)
        {
            xMin /= Screen.width;
            xMax /= Screen.width;
            yMin /= Screen.height;
            yMax /= Screen.height;
        }

        finalString = string.Format("xMin: {0}, yMin: {1}, xMax: {2}, yMax: {3}", xMin, yMin, xMax, yMax);


        Debug.Log(finalString);
    }

    void Capture() {
        GameObject refPoint;

        captureTimer -= Time.deltaTime;
        if (captureTimer <= 0)
        {

            if (initialCube < camAnglesReferenceCubes.Length)
            {

                captureTimer = timerEach;
                refPoint = camAnglesReferenceCubes[initialCube];
                transform.position = refPoint.transform.position;
                transform.LookAt(targetRef.transform);
                initialCube++;
                if (currentTarget != null)
                {
                    if (isRecording)
                        CamCapture(currentTarget);
                    else
                        WriteBoundingBoxToCSV(currentTarget, isNormalized_p);
                }
            }
            else {
                if (initialLight < lightingIntensities.Length)
                {
                    initialCube = 0;
                    initialLight++;
                    foreach (Light lighting in lightings)
                    {
                        lighting.intensity = lightingIntensities[initialLight];
                    }
                }
                else {
                    if (initialTarget < targets.Length)
                    {
                        initialTarget++;
                        initialCube = 0;
                        initialLight = 0;
                        foreach (Light lighting in lightings)
                        {
                            lighting.intensity = lightingIntensities[initialLight];
                        }
                        fileCounter = 0;
                        if (currentTarget != null)
                        {
                            Destroy(currentTarget.gameObject);
                        }
                        currentTarget = Instantiate(targets[initialTarget], refSpawn.transform.position, refSpawn.transform.rotation);
                        currentTarget.transform.parent = obstacleParent.transform;
                    }
                    else {
                        hasEnded = true;   
                    }
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!hasEnded)
            Capture();
    }
}
