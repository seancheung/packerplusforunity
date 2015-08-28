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
	public static extern void CreateImage(int width, int height, [MarshalAs(UnmanagedType.LPStr)] string path);

	[MenuItem("Assets/Test")]
	private static void Test()
	{
		CreateImage(4096, 4096, "Assets/good.png");
		AssetDatabase.Refresh();
	}

	[DllImport(API, EntryPoint = "pack")]
	private static extern void Pack(PackData[] textures, int count, Size maxSize, string path, out Atlas atlas);

	public static void Pack(Texture2D[] textures, AtlasPlus atlas, int width, int height)
	{
		PackData[] data =
			textures.Select(
				t =>
					new PackData
					{
						name = t.name,
						path = AssetDatabase.GetAssetPath(t),
						size = new Size {width = t.width, height = t.height}
					}).ToArray();
		var count = data.Length;
		var size = new Size {width = width, height = height};
		var path = AssetDatabase.GetAssetPath(atlas).Replace(".asset", ".png");
		Atlas info;
		Pack(data, count, size, path, out info);
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
		public string path;
		public Size size;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct Sprite
	{
		public string name;
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
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct PackData
	{
		public string path;
		public string name;
		public Size size;
	}

	#endregion
}