#if UNITY_EDITOR
#define ENABLE_DEBUG
#endif

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using MiniJSON;
using UnityEngine;
#if ENABLE_DEBUG
using UnityEditor;

#endif

#if !UNITY_5 && ENABLE_DEBUG
[InitializeOnLoad]
#endif

public static class PackerWrapper
{
    private const string API = "PackerPlus";

    #region Debug linking

#if UNITY_5
    [InitializeOnLoadMethod]
#else
    static PackerWrapper()
    {
        LinkDebug();
    }
#endif
    private static void LinkDebug()
    {
#if ENABLE_DEBUG
        LinkDebug(logfuncPtr, errorfuncPtr, warningfuncPtr, false);
#endif
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void Debug(string content);

    [DllImport(API, EntryPoint = "link_debug")]
    private static extern void LinkDebug(
        [MarshalAs(UnmanagedType.FunctionPtr)] IntPtr logCal,
        [MarshalAs(UnmanagedType.FunctionPtr)] IntPtr errorCal,
        [MarshalAs(UnmanagedType.FunctionPtr)] IntPtr warningCal, bool logResult);

    private static readonly Debug log = content => UnityEngine.Debug.Log(content);
    private static readonly Debug error = content => UnityEngine.Debug.LogError(content);
    private static readonly Debug warning = content => UnityEngine.Debug.LogWarning(content);

    private static readonly IntPtr logfuncPtr = Marshal.GetFunctionPointerForDelegate(log);
    private static readonly IntPtr errorfuncPtr = Marshal.GetFunctionPointerForDelegate(error);
    private static readonly IntPtr warningfuncPtr = Marshal.GetFunctionPointerForDelegate(warning);

    #endregion

    [DllImport(API, EntryPoint = "pack")]
    private static extern bool Pack([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Texture[] textures, int count,
        Options options, [Out, MarshalAs(UnmanagedType.LPStr)] out string json, DebugOptions debug);

#if UNITY_EDITOR
    /// <summary>
    /// Pack textures(Editor only)
    /// </summary>
    /// <param name="textures"></param>
    /// <param name="atlas"></param>
    /// <param name="options"></param>
    /// <param name="generateJson"></param>
    public static void Pack(Texture2D[] textures, AtlasPlus atlas, Options options, bool generateJson)
    {
        Texture[] data = textures == null
            ? null
            : textures.Select(
                t =>
                    new Texture
                    {
                        name = t.name,
                        path = AssetDatabase.GetAssetPath(t)
                    }).ToArray();
        var count = data == null ? 0 : data.Length;
        string json;
        if (
            !Pack(data, count, options, out json, DebugOptions.None) || string.IsNullOrEmpty(json))
            return;
        if (generateJson)
            File.WriteAllText(Path.ChangeExtension(options.outputPath, "json"), json);
        AssetDatabase.Refresh();
        
        var ja = Json.Deserialize(json) as Dictionary<string, object>;
        if (ja == null)
            return;
        var ta = ja["textures"] as List<object>;
        var sa = ja["sprites"] as List<object>;
        if (ta == null || sa == null)
            return;
        atlas.textures = new TextureInfo[ta.Count];
        for (int i = 0; i < ta.Count; i++)
        {
            Dictionary<string, object> t = (Dictionary<string, object>) ta[i];
            atlas.textures[i] = new TextureInfo();
            atlas.textures[i].width = (int) (long) t["width"];
            atlas.textures[i].height = (int) (long) t["height"];
            atlas.textures[i].texture = AssetDatabase.LoadAssetAtPath<Texture2D>((string) t["path"]);
            if (atlas.textures[i].texture)
                atlas.textures[i].texture.name = (string) t["name"];
        }
        atlas.sprites = new SpriteInfo[sa.Count];
        for (int i = 0; i < sa.Count; i++)
        {
            Dictionary<string, object> s = (Dictionary<string, object>) sa[i];
            atlas.sprites[i] = new SpriteInfo();
            atlas.sprites[i].name = (string) s["name"];
            atlas.sprites[i].section = (int) (long) s["section"];
            var rect = (Dictionary<string, object>) s["rect"];
            var uv = (Dictionary<string, object>) s["uv"];
            atlas.sprites[i].sourceRect = Rect.MinMaxRect((int) (long) rect["xMin"], (int) (long) rect["yMin"],
                (int) (long) rect["xMax"], (int) (long) rect["yMax"]);
            Converter<object, float> converter =
                input => (float) TypeDescriptor.GetConverter(input.GetType()).ConvertTo(input, typeof (float));
            atlas.sprites[i].uvRect = Rect.MinMaxRect(converter(uv["xMin"]), converter(uv["yMin"]),
                converter(uv["xMax"]), converter(uv["yMax"]));
        }
        atlas.maxWidth = options.maxWidth;
        atlas.maxHeight = options.maxHeight;
    }
#endif

    #region Marshal

    [StructLayout(LayoutKind.Sequential)]
    private struct Texture
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string path;
        [MarshalAs(UnmanagedType.LPStr)] public string name;
        public int width;
        public int height;
    }

    public enum ColorDepth
    {
        One = 1,
        Four = 4,
        Eight = 8,
        TrueColor = 24
    }

    public enum Format
    {
        BMP = 1,
        GIF,
        JPG,
        PNG,
        ICO,
        TIF,
        TGA,
        PCX,
        WBMP,
        WMF
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Options
    {
        public int maxWidth;
        public int maxHeight;
        [MarshalAs(UnmanagedType.LPWStr)] public string outputPath;
        public ColorDepth colorDepth;
        public Format format;
        public bool crop;
        public Algorithm algorithm;
    }

    public enum Algorithm
    {
        Plain,
        MaxRects,
        TightRects
    }

    private enum DebugOptions
    {
        None = 0,
        InfoOnly = 1,
        StopAfterLoad = 2 | 1,
        StopAfterComputing = 4 | 1,
        StopAfterPacking = 8 | 1,
        StopAfterJson = 16 | 1,
        SkipJsonT = 32 | 1,
        SkipJsonS = 64 | 1
    }

    #endregion
}