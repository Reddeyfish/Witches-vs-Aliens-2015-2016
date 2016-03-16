﻿using UnityEngine;
using System.Collections;
using UnityEngine.Assertions;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class RaygunAbility : AbstractGenericAbility {

    [SerializeField]
    protected float chargeUpTime;

    [SerializeField]
    protected float stunTime;

    /// <summary>
    /// Pngle of the firing arc when the charging first starts, in radians.
    /// </summary>
    [SerializeField]
    protected float initialArcAngle;

    /// <summary>
    /// Radial length of the firing arc when the charging first starts.
    /// </summary>
    [SerializeField]
    protected float initialArcLength;

    /// <summary>
    /// Number of intermediate (non end-point) points on the outer edge of the arc. Should not be negative.
    /// </summary>
    [SerializeField]
    protected int numArcPoints;

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    Transform rotating;
    Stats myStats;

    protected override void Awake()
    {
        base.Awake();
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        //create our mesh to represent the firing arc
        Mesh arcMesh = new Mesh();
        Assert.IsTrue(numArcPoints >= 0);
        Vector3[] verticies = new Vector3[numArcPoints + 3];
        Vector2[] uvs = new Vector2[verticies.Length];
        int[] triangles = new int[3 * (verticies.Length - 2)];

        //index zero is our centerpoint
        uvs[0] = Vector2.zero;

        verticies = generateArcVertices(initialArcAngle, initialArcLength);

        for(int i = 0; i <= numArcPoints + 1; i++)
        {
            float lerpValue = (float)i / (numArcPoints + 1);
            float uvAngle = lerpValue * Mathf.PI / 2;
            uvs[i + 1] = uvAngle.RadToVector2();
        }

        for (int i = 0; i < verticies.Length - 2; i++)
        {
            int triangleIndex = 3 * i;
            triangles[triangleIndex + 0] = 0;
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = i + 2;
        }

        meshFilter.mesh = arcMesh;
        arcMesh.vertices = verticies;
        arcMesh.uv = uvs;
        arcMesh.triangles = triangles;
    }

    protected override void Start()
    {
        base.Start();
        rotating = transform.root.Find("Rotating");
        myStats = GetComponentInParent<Stats>();
    }

    Vector3[] generateArcVertices(float arcAngle, float arcLength)
    {
        Vector3[] verticies = new Vector3[numArcPoints + 3];

        verticies[0] = Vector2.zero;

        for(int i = 0; i <= numArcPoints + 1; i++)
        {
            float lerpValue = (float) i / (numArcPoints + 1);
            float angle = (lerpValue - 0.5f) * arcAngle / 2;
            Vector2 unitDirectionVector = angle.RadToVector2();
            verticies[i + 1] = arcLength * unitDirectionVector;
        }
        return verticies;
    }

    void updateMesh(float lerpValue)
    {
        float angle = lerpValue * initialArcAngle;
        float length = lerpValue != 0 ? initialArcLength / lerpValue : 1000f;
        meshFilter.mesh.vertices = generateArcVertices(angle, length);
        transform.rotation = rotating.rotation;
    }

    void StopChargingFX()
    {
        meshRenderer.enabled = false;
    }

    protected override void onFire(Vector2 direction)
    {
        active = true;
        meshRenderer.enabled = true;

        Callback.DoLerp(updateMesh, chargeUpTime, this, reverse : true).FollowedBy(() =>
            {
                StopChargingFX();

                direction = transform.right;
                RaycastHit2D[] hits = Physics2D.RaycastAll(this.transform.position, direction);
                for (int i = 0; i < hits.Length; i++)
                {
                    InputToAction input = hits[i].transform.GetComponentInParent<InputToAction>();
                    if (input != null)
                    {
                        Stats otherStats = hits[i].transform.GetComponentInParent<Stats>();
                        if (otherStats.side != myStats.side)
                        {
                            input.DisableMovement(stunTime);
                        }
                    }
                }
                    active = false;
            }, this);
    }

    protected override void Reset(float timeTillActive)
    {
        active = false;
        StopChargingFX();
    }
}