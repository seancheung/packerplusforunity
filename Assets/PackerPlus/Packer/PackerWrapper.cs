#define ENABLE_DEBUG

using System;
using System.Runtime.InteropServices;
using UnityEditor;

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

    [DllImport(API, EntryPoint = "read_image")]
    private static extern IntPtr ReadImage(
        [MarshalAs(UnmanagedType.LPStr)] string path,
        [MarshalAs(UnmanagedType.I8)] out long width, 
        [MarshalAs(UnmanagedType.I8)] out long height);

    public static byte[] ReadImage(string path)
    {
        long width, height;
        var ptr = ReadImage(path, out width, out height);
        var size = width*height;
        if (size <= 0)
            return null;
        byte[] buffer = new byte[size];
        Marshal.Copy(ptr, buffer, 0, buffer.Length);
        return buffer;
    }
}