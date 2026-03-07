using ch.sycoforge.Decal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OnirismSpraypaint;
public class DecalInstance : MonoBehaviour
{
    static readonly Vector3 offset = new(0, 1, 1.5f);
    static readonly Vector3 rotation = new(270, 0, 0);

    public bool Placed { get; private set; } = false;
    public string? Scene { get; private set; }
    public string? Filename { get; private set; }
    EasyDecal? Decal;
    

    /// <summary>
    /// Factory method that creates a new gameobject with a decal.
    /// </summary>
    /// <param name="material">Material to apply to decal.</param>
    /// <param name="scene">Scene decal is placed in, defaults to current scene.</param>
    /// <returns></returns>
    public static DecalInstance New(Material material, string filename)
    {
        var result = new GameObject().AddComponent<DecalInstance>();
        var currentScene = SceneManager.GetActiveScene();
        result.Scene = currentScene.name;
        result.Decal = result.gameObject.AddComponent<EasyDecal>();
        result.SetMaterial(material, filename);
        result.HandleSceneChange(currentScene, 0);
        SceneManager.sceneLoaded += result.HandleSceneChange;
        SpraypaintPlugin.OnUnload += result.OnDestroy;
        return result;
    }

    void HandleSceneChange(Scene scene, LoadSceneMode mode) 
    {
        Log.Debug($"DecalInstance.{this.Filename}.HandleSceneChange({scene.name}, {mode}");
        bool active = scene.name == this.Scene;
        this.gameObject.SetActive(active); 
        Log.Debug($"Decal state set to {this.gameObject.activeSelf}.");
        if (active && this.Placed && this.Decal) { this.Decal.LateBake(3); }
    }

    /// <summary>
    /// Replaces the current material with a new one. 
    /// </summary>
    /// <param name="mat">The new material for this decal to use.</param>
    public void SetMaterial(Material mat, string filename)
    {
        if (!this.Decal) { Log.Error("Called SetMaterial() on an incorrectly built DecalInstance");  return; }

        this.Decal.DecalMaterial = mat;
        this.name = filename;
        this.Filename = filename;
        var tex = mat.mainTexture;
        var (width, height) = (tex.width, tex.height);
        var result = (width: 1.0f, height: 1.0f);
        if (width > height) { result.width = (float)width / height; }
        if (width < height) { result.height = (float)height / width; }
        this.transform.localScale = new Vector3(result.width, 1, result.height);
        Log.Debug($"Scaled DecalInstance to {this.transform.localScale}.");
    }

    /// <summary>
    /// Attaches this decal to the camera so that the player can position it.
    /// </summary>
    public void Pose()
    {
        var camera = Camera.main.transform;
        if (!camera) { Log.Error("Failed to find camera when posing decal."); return; }

        this.transform.SetParent(camera.transform, false);
        this.transform.localPosition = offset;
        this.transform.localRotation = Quaternion.identity;
        this.transform.Rotate(rotation);
    }

    /// <summary>
    /// Attaches this decal to the world at the current position or a specified position.
    /// </summary>
    /// <param name="position">Specific position to use, otherwise maintains current world position.</param>
    public void Place(PositionData? position = null)
    {
        if (!this.Decal) { Log.Warning("Place() was called on a DecalInstance with a null Decal."); return; }

        if (position is null)
        {
            this.Scene = SceneManager.GetActiveScene().name;
            this.transform.SetParent(SpraypaintPlugin.DecalFolder, true);
        }
        else
        {
            this.transform.parent = SpraypaintPlugin.DecalFolder;
            this.Scene = position.scene;
            position.ApplyToTransform(this.transform);
        }
        this.Placed = true;
        HandleSceneChange(SceneManager.GetActiveScene(), 0);
        Log.Debug("DecalInstance.Place() completed.");
    }

    void OnDestroy()
    {
        Log.Debug($"Destroying DecalInstance: {this.name}");
        if (this.gameObject) Destroy(this.gameObject);
        SpraypaintPlugin.OnUnload -= this.OnDestroy;
        SceneManager.sceneLoaded -= this.HandleSceneChange;
    }
}
