﻿using UnityEngine;
using System.Collections;

//single file for all utility extensions

//adds methods to add more options when switching transform parents

public static class TransformExtension
{

    public static void SetParent(this Transform transform, Transform parent, Vector3 newScale, bool worldPositionStays = false)
    {
        transform.SetParent(parent, worldPositionStays);
        transform.localScale = newScale;
    }

    public static void SetParentConstLocalScale(this Transform transform, Transform parent)
    {
        Vector3 localScale = transform.localScale;
        transform.SetParent(parent);
        transform.localScale = localScale;
    }
}

//adds methods to ease conversion between world/screen space, and also with rotations

public static class VectorExtension
{
    public static Vector3 toWorldPoint(this Vector3 screenPoint)
    {
        return Camera.main.ScreenToWorldPoint(screenPoint);
    }

    public static Quaternion ToRotation(this Vector2 dir)
    {
        float _angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        return Quaternion.AngleAxis(_angle, Vector3.forward);
    }

    public static Vector2 normal(this Vector2 dir)
    {
        return new Vector2(-dir.y, dir.x);
    }
}