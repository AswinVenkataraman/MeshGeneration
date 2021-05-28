﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum CurrStage
{
    None,
    DrawingBounds,
    ModifyingBounds
}


public class DrawMesh : MonoBehaviour
{

    public List<Vector3> touchPositions;
    public GameObject lineRendererPrefab;
    LineRenderer currRenderer;
    bool isFocus = true;
    float YDistFromGround = 0.1f;
    float wallHeight = 2f;

    bool isInCollision = false;
    public GameObject vertexObj;
    public GameObject verterParentObj;

    bool pauseDrawing = false;
    CurrStage currStage = CurrStage.None;
    public Material wallMaterial;
    public Material floorMaterial;

    Mesh createdMesh;
    int selectedVerticesIndex = -1;


    private void Awake()
    {
        Camera.main.transform.position = Vector3.up * 10f;
        touchPositions = new List<Vector3>();
        verterParentObj = GameObject.Find("VertexObj");
    }

    private void Update()
    {

        if (pauseDrawing)
            return;


        if (Input.GetMouseButtonDown(0))
            OnMouseClicked();

        if (currStage == CurrStage.DrawingBounds)
        {
            OnMouseHeld();

            if (CheckIntersection())
                isInCollision = true;
            else
                isInCollision = false;
        }
        else if (currStage == CurrStage.ModifyingBounds)
        {
            if(selectedVerticesIndex != -1)
            MoveMesh();
        }

    }

    public void OnMouseClicked()
    {

        if (!isFocus)
        {
            isFocus = true;
            return;
        }
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 5f));
        worldPosition = new Vector3(worldPosition.x, YDistFromGround, worldPosition.z);

        if (currStage == CurrStage.DrawingBounds || currStage == CurrStage.None)
        {

            if (currStage == CurrStage.None)
                currStage = CurrStage.DrawingBounds;

            if (touchPositions.Count >= 2 && Vector3.Distance(worldPosition, touchPositions[0]) <= 1)
            {
                // currRenderer.SetPosition(currRenderer.positionCount - 1, touchPositions[0]);
                currRenderer.loop = true;
                currRenderer.positionCount -= 1;

                 currStage = CurrStage.ModifyingBounds;
                CreateWalls();
                //CreateFloor();
                return;
            }

            if (isInCollision)
                return;

            touchPositions.Add(worldPosition);

            if (currRenderer == null)
                currRenderer = CreateLineRenderer();
            else
                currRenderer.positionCount++;

            GameObject vertexClone = Instantiate(vertexObj, verterParentObj.transform);
            vertexClone.name = "Vertex_" +(currRenderer.positionCount -2);
            vertexClone.transform.position = worldPosition;
        }
        else if(currStage == CurrStage.ModifyingBounds)
        {
            if (selectedVerticesIndex != -1)
            {
                selectedVerticesIndex = -1;
                return;
            }

            //Get the nearest vector of current click.
            int i = 0;
             foreach(Vector3 vertices in createdMesh.vertices)
            {
                if(Vector3.Distance(worldPosition, vertices) < 2f && vertices.y < wallHeight)
                {
                    Debug.Log("Val = " + vertices);
                    selectedVerticesIndex = i;
                }
                i++;
            }

            Debug.Log("selectedVerticesIndex = " + selectedVerticesIndex+ "  createdMesh.vertices.Length = " + createdMesh.vertices.Length);

        }

    }

    public void OnMouseHeld()
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 5f));
        worldPosition = new Vector3(worldPosition.x, YDistFromGround, worldPosition.z);

        if (touchPositions.Count <= 0)
            return;

        if (currStage == CurrStage.DrawingBounds)
            UpdateLineRenderer(touchPositions[touchPositions.Count - 1], worldPosition);

    }

    public void OnMouseButtonReleased()
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        selectedVerticesIndex = -1;
    }

    void MoveMesh()
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldPosition = new Vector3(worldPosition.x, YDistFromGround, worldPosition.z);

        Debug.Log("Move mesh came selectedVerticesIndex = "+ selectedVerticesIndex+ "  createdMesh.vertices.Length  = " + createdMesh.vertices.Length);

        Vector3[] currVertices = createdMesh.vertices;


        for (var i = 0; i < currVertices.Length; i++)
        {
            if (i == selectedVerticesIndex)
            {
                currVertices[i] = worldPosition;
                currRenderer.SetPosition(i / 2, worldPosition);
                verterParentObj.transform.GetChild(i / 2).transform.position = worldPosition;
            }
            else if (i == selectedVerticesIndex + 1)
                currVertices[i] = worldPosition + Vector3.up * wallHeight;
        }

        createdMesh.vertices = currVertices;


        //createdMesh.vertices[selectedVerticesIndex+1] = worldPosition + Vector3.up* wallHeight;
       // createdMesh.vertices[selectedVerticesIndex] = worldPosition;
        createdMesh.RecalculateNormals();
        createdMesh.RecalculateBounds();
        createdMesh.RecalculateTangents();
    }

    public void CreateWalls()
    {
        GameObject wallObj = new GameObject("WallObj");
        wallObj.AddComponent<MeshFilter>();
        wallObj.AddComponent<MeshRenderer>();

        Mesh wallMesh = new Mesh();

        Vector3[] lineVertices = new Vector3[currRenderer.positionCount];
        currRenderer.GetPositions(lineVertices);
        Vector3[] wallVertices = new Vector3[currRenderer.positionCount * 2];


        int i = 0;
        foreach (Vector3 vectorPoints in lineVertices)
        {
            wallVertices[i] = vectorPoints;
            wallVertices[i+1] = vectorPoints + Vector3.up * wallHeight;
            i = i + 2;
        }

        Debug.Log("wallVertices = " + wallVertices.Length);

        int[] triangles = new int[(wallVertices.Length) * 3];

        Debug.Log("wallVertices = " + triangles.Length);

        for (i = 0; i < wallVertices.Length - 2; i+=2)
        {
            Debug.Log("i = " + i);
            Debug.Log("i end = " + (i * 3 + 5));
            
            triangles[i * 3] = i;
            triangles[i * 3 + 1] = i+1;
            triangles[i * 3 + 2] = i+3;

            triangles[i * 3 + 3] = i;
            triangles[i * 3 + 4] = i + 3;
            triangles[i * 3 + 5] = i + 2;
        }


        Debug.Log(" i = " + i);
        triangles[i * 3] = i;
        triangles[i * 3 + 1] = i + 1;
        triangles[i * 3 + 2] = 1;

        triangles[i * 3 + 3] = i;
        triangles[i * 3 + 4] = 1;
        triangles[i * 3 + 5] = 0;

        wallMesh.vertices = wallVertices;
        wallMesh.triangles = triangles;

        wallMesh.RecalculateBounds();
        wallMesh.RecalculateNormals();

        wallObj.GetComponent<MeshFilter>().mesh = wallMesh;
        wallObj.GetComponent<MeshRenderer>().material = wallMaterial;

        createdMesh = wallMesh;
    }

    public void CreateFloor()
    {
        GameObject floorObj = new GameObject("FloorObj");
        floorObj.AddComponent<MeshFilter>();
        floorObj.AddComponent<MeshRenderer>();

        Mesh floorMesh = new Mesh();


        Vector3[] floorVertices = new Vector3[currRenderer.positionCount];

        Debug.Log("floorVertices = " + floorVertices.Length);

        int[] triangles = new int[floorVertices.Length * 3];

        for (int i = 0; i < floorVertices.Length - 2; i += 2)
        {

             {
                triangles[i * 3] = i;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 3;

                triangles[i * 3 + 3] = i;
                triangles[i * 3 + 4] = i + 3;
                triangles[i * 3 + 5] = i + 2;
            }
        }

        floorMesh.vertices = floorVertices;
        floorMesh.triangles = triangles;

        floorMesh.RecalculateBounds();
        floorMesh.RecalculateNormals();

        floorObj.GetComponent<MeshFilter>().mesh = floorMesh;
        floorObj.GetComponent<MeshRenderer>().material = floorMaterial;
    }

    LineRenderer CreateLineRenderer()
    {
        GameObject obj = GameObject.Instantiate(lineRendererPrefab, touchPositions[touchPositions.Count - 1], Quaternion.identity);
        LineRenderer currLineRenderer = obj.GetComponent<LineRenderer>();
        currLineRenderer.positionCount = 1;
        currLineRenderer.SetPosition(currLineRenderer.positionCount - 1, touchPositions[touchPositions.Count - 1]);
        currLineRenderer.positionCount++;
        return currLineRenderer;
    }

    void UpdateLineRenderer(Vector3 startPosition, Vector3 endPosition)
    {
        currRenderer.SetPosition(currRenderer.positionCount - 1, startPosition);
        currRenderer.SetPosition(currRenderer.positionCount - 1, endPosition);
    }

    private void OnApplicationFocus(bool focus)
    {
        if (!focus)
            isFocus = false;

        if (!focus || touchPositions == null || touchPositions.Count <= 0 || currStage != CurrStage.DrawingBounds)
            return;

        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 5f));
        UpdateLineRenderer(touchPositions[touchPositions.Count - 1], worldPosition);
    }


    bool CheckIntersection()
    {

        if (currRenderer == null || currRenderer.positionCount < 4)
            return false;

        bool isColliding = false;

        Vector3 currLineEndPos = currRenderer.GetPosition(currRenderer.positionCount - 2);
        Vector3 currLineStartPos = currRenderer.GetPosition(currRenderer.positionCount - 1);

        Vector3 dirLine1 = currLineStartPos - currLineEndPos;
        Vector3 dirLine2 = currRenderer.GetPosition(currRenderer.positionCount - 3) - currRenderer.GetPosition(currRenderer.positionCount - 4);

        for (int i = 0; i < currRenderer.positionCount - 3; i++)
        {
            if (isLinesIntersect(currLineStartPos, currLineEndPos, currRenderer.GetPosition(i), currRenderer.GetPosition(i + 1)))
            {
                isColliding = true;
                break;
            }
        }

        return isColliding;

    }

    private bool isLinesIntersect(Vector3 CheckLine_Position1, Vector3 CheckLine_Position2, Vector3 Line_Position1, Vector3 Line_Position2)
    {
        // We pick check line first always
        //To check if both lines are interesting, we need to get direction vector
        // Then cross product both with respect to each point
        // Then We will check the angle of both.
        // If the angle sign of both are same, then they are not intersecting

        //CheckLine_Position1 = end point
        //CheckLine_Position2 = before end point

        Vector3 crossVec;

        Vector3 vector1 = CheckLine_Position1 - Line_Position1;
        Vector3 vector2 = CheckLine_Position1 - Line_Position2;

        crossVec = Vector3.Cross(vector1, vector2);
        float Angle1 = crossVec.y;
      //  Debug.Log("Angle 1 = " + Angle1 + " Dot = "+ Vector3.Dot(vector1, vector2));

        vector1 = CheckLine_Position2 - Line_Position1;
        vector2 = CheckLine_Position2 - Line_Position2;
        crossVec = Vector3.Cross(vector1, vector2);
        float Angle2 = crossVec.y;
      //  Debug.Log("Angle 2 = " + Angle2 + " Dot = " + Vector3.Dot(vector1, vector2));

        vector1 = Line_Position1 - CheckLine_Position2;
        vector2 = Line_Position1 - CheckLine_Position1;
        crossVec = Vector3.Cross(vector1, vector2);
        float Angle3 = crossVec.y;
       // Debug.Log("Angle 3 = " + Angle3 + " Dot = " + Vector3.Dot(vector1, vector2));

        vector1 = Line_Position2 - CheckLine_Position2;
        vector2 = Line_Position2 - CheckLine_Position1;
        crossVec = Vector3.Cross(vector1, vector2);
        float Angle4 = crossVec.y;
       // Debug.Log("Angle 4 = " + Angle4 + " Dot = " + Vector3.Dot(vector1, vector2));


        if (Angle1 > 0 && Angle2 > 0)
            return false;
        if (Angle1 < 0 && Angle2 < 0)
            return false;
        if (Angle3 > 0 && Angle4 > 0)
            return false;
        if (Angle3 < 0 && Angle4 < 0)
            return false;

        return true;
    }

}