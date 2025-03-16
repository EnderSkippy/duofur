using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class FFmpegLoader : MonoBehaviour
{
    [DllImport("kernel32")]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("libdl.so.2", EntryPoint = "dlopen")]
    private static extern IntPtr dlopen(string filename, int flags);

    private const int RTLD_NOW = 2;

    public string GetFFmpegPath()
    {
        string ffmpegLibPath = string.Empty;

        if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
        {
            ffmpegLibPath = Path.Combine(Application.streamingAssetsPath, "FFmpeg/Windows/avcodec.dll");
            LoadWindowsLibrary(ffmpegLibPath);
        }
        else if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor)
        {
            ffmpegLibPath = Path.Combine(Application.streamingAssetsPath, "FFmpeg/MacOS/libavcodec.dylib");
            LoadMacLibrary(ffmpegLibPath);
        }
        else if (Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxEditor)
        {
            ffmpegLibPath = Path.Combine(Application.streamingAssetsPath, "FFmpeg/Linux/libavcodec.so");
            LoadLinuxLibrary(ffmpegLibPath);
        }

        if (File.Exists(ffmpegLibPath))
        {
            Debug.Log("Loaded FFmpeg library from: " + ffmpegLibPath);
        }
        else
        {
            Debug.LogError("FFmpeg library not found: " + ffmpegLibPath);
        }

        return ffmpegLibPath;
    }

    private void LoadWindowsLibrary(string path)
    {
        if (LoadLibrary(path) == IntPtr.Zero)
        {
            Debug.LogError("Failed to load Windows FFmpeg library: " + path);
        }
    }

    private void LoadMacLibrary(string path)
    {
        IntPtr handle = dlopen(path, RTLD_NOW);
        if (handle == IntPtr.Zero)
        {
            Debug.LogError("Failed to load macOS FFmpeg library: " + path);
        }
    }

    private void LoadLinuxLibrary(string path)
    {
        IntPtr handle = dlopen(path, RTLD_NOW);
        if (handle == IntPtr.Zero)
        {
            Debug.LogError("Failed to load Linux FFmpeg library: " + path);
        }
    }
}
