using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Enum to seperate stages for conditional checking
public enum CurrStage
{
    None,
    DrawingBounds,
    ModifyingBounds
}

//Script to draw walls and basic floor.
public class DrawMesh : MonoBehaviour
{
    // Touch position list to store the touch positions.
    public List<Vector3> touchPositions;

    //Line renderer related.
    public GameObject lineRendererPrefab;
    LineRenderer currRenderer;

    bool isFocus = true;

    //Distance of points from the ground.
    float YDistFromGround = 0.1f;
    //Height of the walls.
    float wallHeight = 2f;

    //Collission checking variable to prevent moving to next stage when there is collision.
    bool isInCollision = false;
    
    //Vertex visual points objects
    public GameObject vertexObj;
    GameObject verterParentObj;

    //Assign default stage as none
    CurrStage currStage = CurrStage.None;

    //Material Related
    public Material lineMaterial;
    public Material wallMaterial;
    public Material floorMaterial;

    //Mesh related
    Mesh createdWallMesh;
    Mesh createdFloorMesh;

    //Current selected vertex index used for moving during ModifyingBounds Stage
    int selectedVerticesIndex = -1;

    Triangulator triangulator;


    private void Awake()
    {
        Camera.main.transform.position = Vector3.up * 10f;
        touchPositions = new List<Vector3>();
        verterParentObj = GameObject.Find("VertexObj");
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            OnMouseClicked();

        if (currStage == CurrStage.DrawingBounds)
        {
            OnMouseHeld();

            //Check collision during mouse movements

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
        // Don't handle mouse click when is not in focus.
        if (!isFocus)
        {
            isFocus = true;
            return;
        }

        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 5f));
        worldPosition = new Vector3(worldPosition.x, YDistFromGround, worldPosition.z);

        if (currStage == CurrStage.DrawingBounds || currStage == CurrStage.None)
        {
            // Assign DrawingBounds as the initial stage if the curr stage is none.
            if (currStage == CurrStage.None)
                currStage = CurrStage.DrawingBounds;

            if (touchPositions.Count >= 2 && Vector3.Distance(worldPosition, touchPositions[0]) <= 1)
            {
                currRenderer.loop = true;
                currRenderer.positionCount -= 1;
                currStage = CurrStage.ModifyingBounds;
                CreateWalls();
                CreateFloor();
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
            float shortestDistance = float.MaxValue;
             foreach(Vector3 vertices in createdWallMesh.vertices)
            {
                if (Vector3.Distance(worldPosition, vertices) < 2f && vertices.y < wallHeight && Vector3.Distance(worldPosition, vertices)  < shortestDistance)
                {
                    selectedVerticesIndex = i;
                    shortestDistance = Vector3.Distance(worldPosition, vertices);
                }
                i++;
            }

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
        //This is for reseting selected points during mesh movement stage.
        selectedVerticesIndex = -1;
    }


    public bool ContainsPoint(Vector3 p)
    {
        int j = currRenderer.positionCount - 1;
        var inside = false;
        for (int i = 0; i < currRenderer.positionCount; j = i++)
        {
            Vector3 pi = currRenderer.GetPosition(i);
            Vector3 pj = currRenderer.GetPosition(j);
            if (((pi.z <= p.z && p.z < pj.z) || (pj.z <= p.z && p.z < pi.z)) &&
                (p.x < (pj.x - pi.x) * (p.z - pi.z) / (pj.z - pi.z) + pi.x))
                inside = !inside;
        }
        return inside;
    }

    void MoveMesh()
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldPosition = new Vector3(worldPosition.x, YDistFromGround, worldPosition.z);

        Vector3[] currWallVertices = createdWallMesh.vertices;
        Vector2[] UV = createdWallMesh.uv;

        for (var i = 0; i < currWallVertices.Length; i++)
        {
            //This is for the ground vertex
            if (i == selectedVerticesIndex)
            {
                currWallVertices[i] = worldPosition;
                currRenderer.SetPosition(i / 2, worldPosition);
                verterParentObj.transform.GetChild(i / 2).transform.position = worldPosition;
            }
            //This is for the wall vertex
            else if (i == selectedVerticesIndex + 1)
                currWallVertices[i] = worldPosition + Vector3.up * wallHeight; 

            UV[i] = currWallVertices[i];
        }

        //Assign and recalculate bounds.
        createdWallMesh.vertices = currWallVertices;
        createdWallMesh.uv = UV;
        createdWallMesh.RecalculateNormals();
        createdWallMesh.RecalculateBounds();
        createdWallMesh.RecalculateTangents();

        //Same as walls but for floor
        if (createdFloorMesh == null)
            return;

        Vector3[] currFloorVertices = createdFloorMesh.vertices;
        Vector2[] UV1 = createdFloorMesh.uv;
        int[] triangles = new int[(currFloorVertices.Length - 1) * 3];

        for (var i = 0; i < currFloorVertices.Length; i++)
        {
            if (i == selectedVerticesIndex / 2)
            {
                currFloorVertices[i] = worldPosition;
                UV1[i] = new Vector2(currRenderer.GetPosition(i).x, currRenderer.GetPosition(i).z);
            }
        }

        //Assign and recalculate bounds.
        createdFloorMesh.vertices = currFloorVertices;
        triangulator.ReassignVertices(UV1);
        createdFloorMesh.triangles = triangulator.Triangulate();
        createdFloorMesh.uv = UV1;
        createdFloorMesh.RecalculateNormals();
        createdFloorMesh.RecalculateBounds();
        createdFloorMesh.RecalculateTangents();

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
        Vector2[] UV = new Vector2[currRenderer.positionCount * 2];

        int i = 0;

        //Wall has normal touch points and for every touch points there is additional point
        //The additional point is touch point + wall height.
        foreach (Vector3 vectorPoints in lineVertices)
        {
            wallVertices[i] = vectorPoints;
            wallVertices[i+1] = vectorPoints + Vector3.up * wallHeight;
            UV[i] = new Vector2(wallVertices[i].x, wallVertices[i].z);
            UV[i+1] = new Vector2(wallVertices[i+1].x, wallVertices[i+1].z);
            i = i + 2;
        }

        int[] triangles = new int[(wallVertices.Length) * 3];

        //Triangles are drawn in clock wise order to prevent backface culling and mesh not appearing.
        for (i = 0; i < wallVertices.Length - 2; i+=2)
        {
            triangles[i * 3] = i;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 3;

            triangles[i * 3 + 3] = i;
            triangles[i * 3 + 4] = i + 3;
            triangles[i * 3 + 5] = i + 2;
        }

        //Drawing last traingle and mapping it to the origin.
        triangles[i * 3] = i;
        triangles[i * 3 + 1] = i + 1;
        triangles[i * 3 + 2] = 1;

        triangles[i * 3 + 3] = i;
        triangles[i * 3 + 4] = 1;
        triangles[i * 3 + 5] = 0;

        //Assign the vertices
        wallMesh.vertices = wallVertices;
        wallMesh.uv = UV;
        wallMesh.triangles = triangles;
        wallObj.GetComponent<MeshFilter>().mesh = wallMesh;
        wallObj.GetComponent<MeshRenderer>().material = wallMaterial;

        //Recalculate the bounds
        wallMesh.RecalculateBounds();
        wallMesh.RecalculateTangents();

        createdWallMesh = wallMesh;
    }
  
    public void CreateFloor()
    {

        GameObject floorObj = new GameObject("FloorObj");
        floorObj.AddComponent<MeshFilter>();
        floorObj.AddComponent<MeshRenderer>();

        Mesh floorMesh = new Mesh();

        Vector3[] floorVertices = new Vector3[currRenderer.positionCount];
        Vector2[] UV = new Vector2[currRenderer.positionCount];

        int i = 0;

        for(i = 0; i< floorVertices.Length;i++)
        {
            floorVertices[i] = currRenderer.GetPosition(i);
            UV[i] = new Vector2(currRenderer.GetPosition(i).x, currRenderer.GetPosition(i).z);
        }

        int[] triangles = new int[(floorVertices.Length-1) * 3];

        int j = 0;
        for (i = 1; i <= floorVertices.Length - 1; i++)
        {

            triangles[j] = i-1;
            triangles[j+1] = i;
            if((i + 1)>= floorVertices.Length)
                triangles[j + 2] = 0;
            else 
                triangles[j + 2] = i+1;

            j += 3;
        }
        //Assign values to the mesh
        floorMesh.vertices = floorVertices;

        if(triangulator==null)
            triangulator = new Triangulator(UV);
        else
            triangulator.ReassignVertices(UV);
        floorMesh.triangles = triangulator.Triangulate();
        floorMesh.uv = UV;

        //Recalculate bounds
        floorMesh.RecalculateTangents();
        floorMesh.RecalculateBounds();
        floorMesh.RecalculateNormals();

        floorObj.GetComponent<MeshFilter>().mesh = floorMesh;
        floorObj.GetComponent<MeshRenderer>().material = floorMaterial;

        createdFloorMesh = floorMesh;
    }

    LineRenderer CreateLineRenderer()
    {
        //Create the line renderer based on the touchPoints.
        GameObject obj = GameObject.Instantiate(lineRendererPrefab, touchPositions[touchPositions.Count - 1], Quaternion.identity);
        LineRenderer currLineRenderer = obj.GetComponent<LineRenderer>();
        currLineRenderer.material = lineMaterial;
        currLineRenderer.positionCount = 1;
        currLineRenderer.SetPosition(currLineRenderer.positionCount - 1, touchPositions[touchPositions.Count - 1]);
        currLineRenderer.positionCount++;
        return currLineRenderer;
    }

    void UpdateLineRenderer(Vector3 startPosition, Vector3 endPosition)
    {
        //Update the line renderer's current editing point based on touchPosition.
        currRenderer.SetPosition(currRenderer.positionCount - 1, startPosition);
        currRenderer.SetPosition(currRenderer.positionCount - 1, endPosition);
    }

    private void OnApplicationFocus(bool focus)
    {
        //This is to resume the mesh plotting when cursor is moved away from the play window.

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

        // Skip the last point as we are checking it seperately. If we check that in this, it will always be colliding,
        // since it touches the last sharing point.
        for (int i = 0; i < currRenderer.positionCount - 3; i++)
        {
            if (isLinesIntersect(currLineStartPos, currLineEndPos, currRenderer.GetPosition(i), currRenderer.GetPosition(i + 1)))
            {
                isColliding = true;
                break;
            }
        }

        //Checking the last line with the points before it.
        if (CheckPointIsInsideLine(currLineStartPos, currRenderer.GetPosition(currRenderer.positionCount - 2), currRenderer.GetPosition(currRenderer.positionCount - 3)) ||
            CheckPointIsInsideLine(currRenderer.GetPosition(currRenderer.positionCount - 3), currLineStartPos, currLineEndPos)
            )
        {
            isColliding = true;
        }

        return isColliding;
    }


    bool CheckPointIsInsideLine(Vector3 point, Vector3 linePoint1, Vector3 linePoint2)
    {

        // If point is between linePoint1 and linePoint2
        // Normal checking if point is between 2 lines with their bounds.

        if (point.x <= Mathf.Max(linePoint1.x, linePoint2.x) && point.x >= Mathf.Min(linePoint1.x, linePoint2.x) &&
            point.z <= Mathf.Max(linePoint1.z, linePoint2.z) && point.z >= Mathf.Min(linePoint1.z, linePoint2.z))
            return true;

        return false;
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

        vector1 = CheckLine_Position2 - Line_Position1;
        vector2 = CheckLine_Position2 - Line_Position2;
        crossVec = Vector3.Cross(vector1, vector2);
        float Angle2 = crossVec.y;

        vector1 = Line_Position1 - CheckLine_Position2;
        vector2 = Line_Position1 - CheckLine_Position1;
        crossVec = Vector3.Cross(vector1, vector2);
        float Angle3 = crossVec.y;

        vector1 = Line_Position2 - CheckLine_Position2;
        vector2 = Line_Position2 - CheckLine_Position1;
        crossVec = Vector3.Cross(vector1, vector2);
        float Angle4 = crossVec.y;

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
