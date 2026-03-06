using Newtonsoft.Json;
using System;
using UnityEngine;

namespace OnirismSpraypaint;

[Serializable]
public class PositionData
{
    public FakeVector3 position;
    public FakeQuaternion rotation;
    public FakeVector3 scale;
    public string scene;

    #pragma warning disable CS8618 
    [JsonConstructor] public PositionData() { }
    #pragma warning restore CS8618 

    public PositionData(Transform transform, string? scene = null)
    {
        this.position = new FakeVector3(transform.position);
        this.rotation = new FakeQuaternion(transform.rotation);
        this.scale = new FakeVector3(transform.localScale);
        this.scene = scene ?? transform.gameObject.scene.name;
    }

    public Transform ApplyToTransform(Transform transform)
    {
        transform.position = position.ToVector3();
        transform.rotation = rotation.ToQuaternion();
        transform.localScale = scale.ToVector3();
        return transform;
    }
}


[Serializable]
public class FakeVector3
{
    public float x, y, z;

    [JsonConstructor] public FakeVector3() { }
    public FakeVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public FakeVector3(Vector3 v3)
    {
        this.x = v3.x;
        this.y = v3.y;
        this.z = v3.z;
    }

    public Vector3 ToVector3() => new Vector3(x, y, z);
}

[Serializable]
public class FakeQuaternion
{
    public float w, x, y, z;

    [JsonConstructor] public FakeQuaternion() { }

    public FakeQuaternion(Quaternion quat)
    {
        this.w = quat.w;
        this.x = quat.x;
        this.y = quat.y;
        this.z = quat.z;
    }

    public Quaternion ToQuaternion() => new Quaternion(x, y, z, w);
}
