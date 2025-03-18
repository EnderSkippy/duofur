using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class FFmpegLoader : MonoBehaviour
{

    [DllImport("libdl.so.2", EntryPoint = "dlopen")]
    private static extern IntPtr dlopen(string filename, int flags);

    private const int RTLD_NOW = 2;

    public string GetFFmpegPath()
    {
        string ffmpegPath = string.Empty;

        if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
        {
            ffmpegPath = Path.Combine(Application.streamingAssetsPath, "FFmpeg/Windows/ffmpeg.exe");
        }
        else if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor)
        {
            ffmpegPath = Path.Combine(Application.streamingAssetsPath, "FFmpeg/MacOS/libavcodec.dylib");
            LoadMacLibrary(ffmpegPath);
        }
        else if (Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxEditor)
        {
            ffmpegPath = Path.Combine(Application.streamingAssetsPath, "FFmpeg/Linux/ffmpeg");
        }

        return ffmpegPath;
    }

    // Likely doesn't work, I don't have a mac on me so I wouldn't know.
    private void LoadMacLibrary(string path)
    {
        IntPtr handle = dlopen(path, RTLD_NOW);
        if (handle == IntPtr.Zero)
        {
            Debug.LogError("Failed to load macOS FFmpeg library: " + path);
        }
    }
}