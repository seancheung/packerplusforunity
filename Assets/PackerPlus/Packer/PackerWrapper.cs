#define ENABLE_DEBUG

using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

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
        [MarshalAs(UnmanagedType.FunctionPtr)] IntPtr warningCal,
        [MarshalAs(UnmanagedType.I1)] bool logResult);

    private static readonly Debug log = content => UnityEngine.Debug.Log(content);
    private static readonly Debug error = content => UnityEngine.Debug.LogError(content);
    private static readonly Debug warning = content => UnityEngine.Debug.LogWarning(content);

    private static readonly IntPtr logfuncPtr = Marshal.GetFunctionPointerForDelegate(log);
    private static readonly IntPtr errorfuncPtr = Marshal.GetFunctionPointerForDelegate(error);
    private static readonly IntPtr warningfuncPtr = Marshal.GetFunctionPointerForDelegate(warning);

    #endregion

    [DllImport(API, EntryPoint = "create_empty")]
    public static extern void CreateImage([In] int width, [In] int height,
        [In, MarshalAs(UnmanagedType.LPStr)] string path,
        ColorDepth depth,
        Format format, Color color);

    [DllImport(API, EntryPoint = "pack")]
    private static extern void Pack([In] IntPtr textures, [In] int count, [In] Size maxSize, [In] string path,
        [In, Out] IntPtr atlas,
        [In] ColorDepth depth, [In] Format format);

    public static void Pack(Texture2D[] textures, AtlasPlus atlas, int width, int height, ColorDepth depth,
        Format format)
    {
        Texture[] data =
            textures.Select(
                t =>
                    new Texture
                    {
                        name = t.name,
                        path = AssetDatabase.GetAssetPath(t),
                        size = new Size {width = t.width, height = t.height}
                    }).ToArray();
        var count = data.Length;
        var size = new Size {width = width, height = height};
        var path = AssetDatabase.GetAssetPath(atlas).Replace(".asset", format.ToString().ToLower());
        Atlas info = new Atlas();

        IntPtr textureIntPtr = ArrayToIntPtr(data);
        IntPtr atlasPtr = Atlas.MarshalManagedToNative(info);
        try
        {
            Pack(textureIntPtr, count, size, path, atlasPtr, depth, format);
            info = (Atlas) Atlas.MarshalNativeToManaged(atlasPtr);
        }
        finally
        {
            Marshal.FreeHGlobal(textureIntPtr);
            Marshal.FreeHGlobal(atlasPtr);
        }

        atlas.maxWidth = info.maxSize.width;
        atlas.maxHeight = info.maxSize.height;
        atlas.textures = new TextureInfo[info.textureCount];
        for (int i = 0; i < info.textureCount; i++)
        {
            atlas.textures[i] = new TextureInfo
            {
                width = info.textures[i].size.width,
                height = info.textures[i].size.height,
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(info.textures[i].path)
            };
        }
        atlas.sprites = new SpriteInfo[info.spriteCount];
        for (int i = 0; i < info.spriteCount; i++)
        {
            atlas.sprites[i] = new SpriteInfo
            {
                name = info.sprites[i].name,
                section = info.sprites[i].section,
                sourceRect = new Rect(0, 0, info.sprites[i].size.width, info.sprites[i].size.height),
                uvRect =
                    Rect.MinMaxRect(info.sprites[i].uv.xMin, info.sprites[i].uv.yMin, info.sprites[i].uv.xMax,
                        info.sprites[i].uv.yMax)
            };
        }
    }

    private static IntPtr ArrayToIntPtr(Array array)
    {
        if (array.Length == 0)
            return IntPtr.Zero;
        var size = Marshal.SizeOf(array.GetValue(0));
        IntPtr mem = Marshal.AllocHGlobal(size*array.Length);
        for (int i = 0; i < array.Length; i++)
        {
            Marshal.StructureToPtr(array.GetValue(i), mem, false);
            mem = new IntPtr((long) mem + size);
        }
        return mem;
    }

    #region Marshal

    [StructLayout(LayoutKind.Sequential)]
    private struct UVRect
    {
        public float xMin;
        public float yMin;
        public float xMax;
        public float yMax;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct Size
    {
        [FieldOffset(0)] public int width;
        [FieldOffset(0)] public int height;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Texture
    {
        [MarshalAs(UnmanagedType.LPStr)] public string path;
        [MarshalAs(UnmanagedType.LPStr)] public string name;
        public Size size;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Sprite
    {
        [MarshalAs(UnmanagedType.LPStr)] public string name;
        public UVRect uv;
        public Size size;
        public int section;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Atlas
    {
        public Size maxSize;
        public int textureCount;
        public Texture[] textures;
        public int spriteCount;
        public Sprite[] sprites;

        public static object MarshalNativeToManaged(IntPtr pNativeData)
        {
            Atlas atlas = new Atlas();
            atlas.maxSize = (Size) Marshal.PtrToStructure(pNativeData, typeof (Size));
            atlas.textureCount = Marshal.ReadInt32(pNativeData, Marshal.SizeOf(typeof (Size)));
            atlas.textures = new Texture[atlas.textureCount];

            pNativeData = new IntPtr(pNativeData.ToInt32() + Marshal.SizeOf(typeof(Size)) + Marshal.SizeOf(typeof(int)));
            for (int i = 0; i < atlas.textureCount; i++)
            {
                Texture t = (Texture) Marshal.PtrToStructure(pNativeData, typeof (Texture));
                atlas.textures[i] = t;
                pNativeData = new IntPtr(pNativeData.ToInt32() + Marshal.SizeOf(typeof(Texture)));
            }

            atlas.spriteCount = Marshal.ReadInt32(pNativeData,
                Marshal.SizeOf(typeof (Size)) + Marshal.SizeOf(typeof (int)) +
                atlas.textureCount*Marshal.SizeOf(typeof (Texture)));

            pNativeData =
                new IntPtr(pNativeData.ToInt32() + Marshal.SizeOf(typeof (Size)) + 2*Marshal.SizeOf(typeof (int)) +
                           atlas.textureCount*Marshal.SizeOf(typeof (Texture)));
            for (int i = 0; i < atlas.spriteCount; i++)
            {
                Sprite s = (Sprite)Marshal.PtrToStructure(pNativeData, typeof(Sprite));
                atlas.sprites[i] = s;
                pNativeData = new IntPtr(pNativeData.ToInt32() + Marshal.SizeOf(typeof(Sprite)));
            }

            return atlas;
        }

        public static IntPtr MarshalManagedToNative(object managedObj)
        {
            Atlas atlas = (Atlas) managedObj;
            IntPtr ptr = Marshal.AllocCoTaskMem(GetNativeDataSize(atlas));
            if (IntPtr.Zero == ptr)
            {
                throw new Exception("Could not allocate memory");
            }

            Marshal.StructureToPtr(atlas.maxSize, ptr, false);
            Marshal.WriteInt32(ptr, Marshal.SizeOf(typeof(Size)), atlas.textureCount);
            ptr = new IntPtr(ptr.ToInt32() + Marshal.SizeOf(typeof(Size)));
            for (int i = 0; i < atlas.textureCount; i++)
            {
                Marshal.StructureToPtr(atlas.textures[i], ptr, false);
                ptr = new IntPtr(ptr.ToInt32() + Marshal.SizeOf(typeof(Texture)));
            }
            Marshal.WriteInt32(ptr,
                Marshal.SizeOf(typeof (Size)) + Marshal.SizeOf(typeof (int)) +
                atlas.textureCount * Marshal.SizeOf(typeof(Texture)), atlas.spriteCount);
            ptr = new IntPtr(ptr.ToInt32() + Marshal.SizeOf(typeof (Size)) + 2*Marshal.SizeOf(typeof (int)) +
                                   atlas.textureCount * Marshal.SizeOf(typeof(Texture)));
            for (int i = 0; i < atlas.spriteCount; i++)
            {
                Marshal.StructureToPtr(atlas.sprites[i], ptr, false);
                ptr = new IntPtr(ptr.ToInt32() + Marshal.SizeOf(typeof(Sprite)));
            }

            return ptr;
        }

        static int GetNativeDataSize(Atlas atlas)
        {
            int size = Marshal.SizeOf(typeof(Size));
            size += Marshal.SizeOf(typeof(int))*2;
            if (atlas.textures != null)
                size += Marshal.SizeOf(typeof(Texture)) * atlas.textures.Length;
            if (atlas.sprites != null)
                size += Marshal.SizeOf(typeof(Sprite)) * atlas.sprites.Length;
            return size;
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Color
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;

        public Color(byte r, byte g, byte b, byte a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static implicit operator Color(Color32 color)
        {
            return new Color(color.r, color.g, color.b, color.a);
        }
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

    #endregion
}