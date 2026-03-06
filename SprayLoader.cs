using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace OnirismSpraypaint;
public class SprayLoader
{
    //TODO: use directory relative to installation directory
    const string SprayDirectory = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Onirism\\Sprays";
    static readonly Dictionary<string, Material> LoadedSprays = [];

    /// <summary>
    /// The currently selected material to apply to new/inprogress sprays. Returns null if no sprays are loaded.
    /// </summary>
    //public static Material? Current => matIndex == -1 ? null : LoadedSprays.ElementAt(matIndex).Value;
    public static (string, Material)? Current()
    {
        if (matIndex == -1) return null;
        
        var kvp = LoadedSprays.ElementAt(matIndex);
        return (kvp.Key, kvp.Value);

    }
    static int matIndex = -1;

    /// <summary>
    /// Sets SprayFiles.Current to the next material in the list.
    /// </summary>
    public static void Cycle() 
    {
        var count = LoadedSprays.Count;
        if (count == 0) matIndex = -1;
        else matIndex = (matIndex + 1) % count;
    } 

    /// <summary>
    /// Loads & converts all .jpg and .png files in the spray directory into materials.
    /// </summary>
    public static void LoadImages()
    {
        //TODO: async version
        LoadedSprays.Clear();
        Directory.CreateDirectory(SprayDirectory);
        Log.Debug("Loading materials...");
        var files = Directory.EnumerateFiles(SprayDirectory);
        files
            .Where(path => path.EndsWith(".png") || path.EndsWith(".jpg"))
            .Select(path => (filename: Path.GetFileNameWithoutExtension(path), mat: FileToMaterial(path)))
            .ToList()
            .ForEach(tup => LoadedSprays.Add(tup.filename, tup.mat));
        Log.Debug("Loading spray position data...");
        files
            .Where(path => path.EndsWith(".json"))
            .Select(FileToMetadata)
            .SelectMany(list => list)
            .ToList()
            .ForEach(LoadSaved);
        Log.Info($"Loaded {LoadedSprays.Count} sprays");
        Cycle();
    }



    /// <summary>
    /// Stores all placed decals as .json files. Overwrites existing files. 
    /// </summary>
    public static void Save()
    {
        SpraypaintPlugin.DecalFolder
            .GetComponentsInChildren<DecalInstance>(true)
            .GroupBy(decal => decal.Filename)
            .Select(grouping => grouping.ToList())
            .ToList()
            .ForEach(SaveDecal);
    }

    static void SaveDecal(List<DecalInstance> instances)
    {
        if (instances.FirstOrDefault() is not DecalInstance first) { Log.Warning("An empty DecalInstance list was asked to be saved."); return; }
        if (first.Filename is not string filename) { Log.Warning($"Trying to save but the DecalInstance had no Filename."); return; }

        var jsonFilename = filename + ".json";
        var list = instances
            .Select(decal => new PositionData(decal.transform, decal.Scene))
            .ToList();
        var json = JsonConvert.SerializeObject(list);
        var path = Path.Combine(SprayDirectory, jsonFilename);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Place a decal in the world at a specified scene & position.
    /// </summary>
    /// <param name="save"></param>
    public static void LoadSaved(SprayPositionData save)
    {
        if (save.Filename is not string filename) { Log.Warning("Tried to load SprayPositionData with null filename"); return; }

        filename = Path.GetFileNameWithoutExtension(filename);
        if (!LoadedSprays.TryGetValue(filename, out var mat)) { Log.Warning($"Material for file {filename} is not loaded."); return; }

        DecalInstance
            .New(mat, filename)
            .Place(save.Position);
    }

    /// <summary>
    /// Reads spray position data from file. 
    /// </summary>
    /// <param name="filepath">Path of file to load.</param>
    /// <returns>List of all spray position data.</returns>
    public static List<SprayPositionData> FileToMetadata(string filepath)
    {
        string filename = Path.GetFileName(filepath);
        var json = File.ReadAllText(filepath);
        var positionData = JsonConvert.DeserializeObject<List<PositionData>>(json);
        if (positionData is null) return [];
        
        
        else return positionData.Select(pd => new SprayPositionData(filename, pd)).ToList();
    }

    /// <summary>
    /// Loads the image file at path and returns a material using that image. 
    /// </summary>
    /// <param name="path">File path</param>
    /// <returns>Material with </returns>
    public static Material FileToMaterial(string path)
    {
        //TODO: async version 
        Log.Debug($"Loading spray from path: {path}");
        var data = File.ReadAllBytes(path);
        var texture = new Texture2D(1, 1);
        ImageConversion.LoadImage(texture, data);
        var material = CreateUber(texture);
        Log.Info("Material construction complete.");
        return material;
    }

    /// <summary>
    /// Creates a new instance of a material with the specified texture using an Uber shader.
    /// </summary>
    /// <param name="texture">Texture to use as the material.mainTexture. </param>
    /// <returns>A new Material using Uber Metallic shader with the specified texture.</returns>
    public static Material CreateUber(Texture texture)
    {
        var mat = new Material(Shader.Find("UBER - Metallic Setup/ Core"));
        mat.mainTexture = texture;
        mat.SetTexture("_EmissionMap", texture);
        mat.SetColor("_Color", new Color(1f, 1f, 1f, 1f));
        mat.SetColor("_EmissionColor", new Color(0.4f, 0.4f, 0.4f, 1f));
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Glossiness", 0.4f);
        mat.EnableKeyword("_WETNESS_NONE");
        mat.EnableKeyword("_EMISSION_TEXTURED");
        mat.EnableKeyword("_OCCLUSION_FROM_ALBEDO_ALPHA");
        mat.EnableKeyword("_ALPHATEST_ON");
        return mat;
    }
}
