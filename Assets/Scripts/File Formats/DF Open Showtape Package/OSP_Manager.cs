using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Global;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

public class OSP_Manager : MonoBehaviour
{
    ShowController _showController;

    private OpenShowtapePackage _currentPackage;

    private string _ffmpeg;
    private void Awake()
    {
        _showController = GetComponent<ShowController>();
        
        FFmpegLoader ffmpegLoader = new();
        _ffmpeg = ffmpegLoader.GetFFmpegPath();
    }
    public void UpdateOsp()
    {
        int frameIndex = (int)(_showController.referenceAudio.time * _currentPackage.frameRate);


        if (frameIndex < _currentPackage.frames.Length)
        {
            for (int i = 0; i < _currentPackage.frames[frameIndex].Length; i++)
            {
                if (_currentPackage.frames[frameIndex][i] <= 150)
                {
                    _showController.topDrawer[_currentPackage.frames[frameIndex][i]] = true;
                }
                else
                {
                    _showController.bottomDrawer[_currentPackage.frames[frameIndex][i] - 150] = true;
                }
                
            }
        }
    }

    public async Task Load(string path, IProgress<float> progress = null)
    {
        string tempOggPath = null;
        
        try
        {
            progress?.Report(1);
            Debug.Log($"OSP Manager: Loading showtape at path {path}");
            string contents = await File.ReadAllTextAsync(path);
            progress?.Report(10);
            _currentPackage = JsonConvert.DeserializeObject<OpenShowtapePackage>(contents);
            Debug.Log("OSP Manager: Successfully deserialized JSON");
            progress?.Report(30);

            byte[] audioData = Convert.FromBase64String(_currentPackage.audioData.data);
            progress?.Report(50);

            tempOggPath = Path.GetTempFileName();
            Debug.Log($"OSP Manager: Created temporary mp3 file at {tempOggPath}");
            await File.WriteAllBytesAsync(tempOggPath, audioData);
            
            progress?.Report(70);

            AudioClip audioClip = await UriToAudioClipAsync(tempOggPath);
            
            if (!audioClip) throw new Exception("OSP Manager: Failed to create audio clip");
            
            _showController.referenceAudio.clip = audioClip;
            Debug.Log("OSP Manager: Converted audio to AudioClip & set referenceAudio.clip");

            _showController.format = Format.OpenShowtapePackage;
            
            _showController.AfterLoad();
            progress?.Report(0);
            
            File.Delete(tempOggPath);
        }
        catch (Exception e)
        {
            Debug.LogError("OSP Manager: Failed to load showtape\n" + e.Message + "\n" + e.StackTrace);
            if (tempOggPath != null) File.Delete(tempOggPath);
            Debug.Log("OSP Manager: Cleared temporary files");
            progress?.Report(0);
        }
    }

    private static async Task<AudioClip> UriToAudioClipAsync(string path)
    {
        if (!File.Exists(path))
        {
            throw new Exception("OSP Manager: Path provided as Uri is invalid");
        }

        var tcs = new TaskCompletionSource<AudioClip>();

        using (var uwr = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.OGGVORBIS))
        {
            ((DownloadHandlerAudioClip)uwr.downloadHandler).streamAudio = false;

            await uwr.SendWebRequest();

            if (!string.IsNullOrEmpty(uwr.error) && uwr.error.Contains("Network Error"))
            {
                Debug.LogError(uwr.error);
                tcs.SetException(new Exception(uwr.error));
            }
            else
            {
                DownloadHandlerAudioClip dlHandler = (DownloadHandlerAudioClip)uwr.downloadHandler;

                if (dlHandler.isDone)
                {
                    AudioClip audioClip = dlHandler.audioClip;

                    if (audioClip != null)
                    {
                        tcs.SetResult(audioClip);
                        Debug.Log("OSP Manager: Successfully converted audio to AudioClip");
                    }
                    else
                    {
                        tcs.SetException(new Exception("OSP Manager: Couldn't find a valid AudioClip"));
                    }
                }
                else
                {
                    tcs.SetException(new Exception("OSP Manager: The download process is not completely finished."));
                }
            }
        }

        return await tcs.Task;
    }
    
    /// <summary>
    ///     Writes an OpenShowtapePackage into a JSON encoded file
    /// </summary>
    /// <param name="filePath">Where the file should be written</param>
    /// <param name="showtape">The OpenShowtapePackage to use</param>
    public void Save(string filePath, OpenShowtapePackage showtape)
    {
        JsonSerializer serializer = new()
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        using StreamWriter sw = new(filePath);
        using JsonWriter writer = new JsonTextWriter(sw);
        serializer.Serialize(writer, showtape);
    }

    /// <summary>
    ///     Attempts to convert filePath into an OpenShowtapePackage
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    public async Task<OpenShowtapePackage> ConvertFileAsync(string filePath, IProgress<float> progress = null)
    {
        string tempWav = null;
        string tempOgg = null;
        string tempOgv = null;

        try
        {
            OpenShowtapePackage showtape = new();
            if (File.Exists(filePath))
            {
                string ex = Path.GetExtension(filePath);
                if (ex == ".rshw" || ex == ".cshw" || ex == ".sshw") // RR .*shw Converter
                {
                    Debug.Log($"OSP Manager: Beginning conversion of {ex} file to OSP");

                    progress?.Report(1);
                    showtape.metadata = new OspMetadata
                    {
                        title = Path.GetFileNameWithoutExtension(filePath),
                        additionalInfo = $"Converted file from {ex}"
                    };

                    rshwFormat shw = await Task.Run(() => rshwFormat.Read(filePath));
                    Debug.Log($"OSP Manager: Loaded .*shw showtape at {filePath}");
                    progress?.Report(10);

                    tempWav = Path.GetTempFileName() + ".wav";
                    tempOgg = Path.GetTempFileName() + ".ogg";

                    await File.WriteAllBytesAsync(tempWav, shw.audioData);
                    progress?.Report(15); // Update after writing WAV file

                    // Convert WAV to OGG & save
                    progress?.Report(20);
                    await ConvertAudioToOggAsync(tempWav, tempOgg, showtape, progress);

                    File.Delete(tempOgg);
                    File.Delete(tempWav);

                    // Process signal to frames
                    await ConvertSignalToFramesAsync(shw, showtape, progress);

                    // Handle video conversion if available
                    await ConvertVideoAsync(filePath, showtape, progress);

                    Debug.Log($"OSP Manager: {ex} to OSP conversion complete!");
                    progress?.Report(0);
                    return showtape;
                }

                return null;
            }

            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"OSP Manager: Conversion failed with exception:\n{e}");
            CleanUpTempFiles(tempWav, tempOgg, tempOgv);
            progress?.Report(0);
            return null;
        }
    }

    private async Task ConvertAudioToOggAsync(string tempWav, string tempOgg, OpenShowtapePackage showtape, IProgress<float> progress)
    {
        ProcessStartInfo processStartInfo = new()
        {
            FileName = _ffmpeg,
            Arguments = $"-i \"{tempWav}\" -vn -b:a 128k \"{tempOgg}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = Process.Start(processStartInfo);
        if (process != null)
        {
            string standardError = await process.StandardError.ReadToEndAsync();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Debug.LogError($"FFmpeg exited with error code {process.ExitCode}\n{standardError}");
                throw new Exception("OGG conversion failed.");
            }
            
            progress?.Report(30);
        }

        // extract audio metadata
        ProcessStartInfo ffmpegInfo = new()
        {
            FileName = _ffmpeg,
            Arguments = $"-i \"{tempWav}\"",
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process infoProcess = Process.Start(ffmpegInfo);
        if (infoProcess != null)
        {
            string infoOutput = await infoProcess.StandardError.ReadToEndAsync();
            infoProcess.WaitForExit();

            showtape.audioData = new OspAudioData
            {
                channels = ExtractIntFromFFmpegOutput(infoOutput, "Audio:.*?(\\d+) channels"),
                sampleRate = ExtractIntFromFFmpegOutput(infoOutput, "(\\d+) Hz"),
                bitRate = ExtractIntFromFFmpegOutput(infoOutput, "(\\d+) kb/s"),
                data = Convert.ToBase64String(await File.ReadAllBytesAsync(tempOgg))
            };
        }
    }

    // helper function to extract integer values from ffmpeg output
    private int ExtractIntFromFFmpegOutput(string output, string pattern)
    {
        var match = System.Text.RegularExpressions.Regex.Match(output, pattern);
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }


    private async Task ConvertSignalToFramesAsync(rshwFormat shw, OpenShowtapePackage showtape, IProgress<float> progress)
    {
        showtape.frameRate = 60;

        showtape.frames = await Task.Run(() =>
        {
            var newSignals = new List<List<int>>();
            int countLength = 0;
            int totalSignals = shw.signalData.Length;

            if (totalSignals > 0 && shw.signalData[0] != 0)
            {
                countLength = 1;
                newSignals.Add(new List<int>());
            }

            for (int i = 0; i < totalSignals; i++)
            {
                if (shw.signalData[i] == 0)
                {
                    countLength += 1;
                    newSignals.Add(new List<int>());
                }
                else
                {
                    newSignals[countLength - 1].Add(shw.signalData[i] - 1);
                }

                if (i % (totalSignals / 100) == 0)
                {
                    float percentage = 40 + (float)(i) / (totalSignals * 60) * 60;
                    UnityMainThreadDispatcher.Dispatcher.Enqueue(() => progress?.Report(percentage));
                }
            }

            return newSignals.Select(lst => lst.ToArray()).ToArray();
        });

        Debug.Log("OSP Manager: Signal to frame conversion complete.");
        progress?.Report(50);
    }

    private async Task ConvertVideoAsync(string filePath, OpenShowtapePackage showtape, IProgress<float> progress)
    {
        string videoPath = Path.Join(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + ".mp4");
        if (File.Exists(videoPath))
        {
            Debug.Log($"OSP Manager: Video found at {videoPath}, beginning conversion. This may take a while.");
            string tempOgv = Path.GetTempFileName() + ".ogv";

            ProcessStartInfo videoProcessStartInfo = new()
            {
                FileName = _ffmpeg,
                Arguments = $"-i \"{videoPath}\" -c:v libtheora -q:v 5 -preset fast -an \"{tempOgv}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = false
            };

            using Process process = Process.Start(videoProcessStartInfo);
            if (process != null)
            {
                Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();

                await Task.WhenAny(Task.Run(() => process.WaitForExit()), Task.WhenAll(standardOutputTask, standardErrorTask));

                string standardError = standardErrorTask.Result;

                if (!string.IsNullOrEmpty(standardError))
                {
                    Debug.LogError($"FFmpeg Error Output:\n{standardError}");
                }

                if (process.ExitCode != 0)
                {
                    Debug.LogError($"FFmpeg exited with error code {process.ExitCode}\n{standardError}");
                    File.Delete(tempOgv);
                    progress?.Report(0);
                    return;
                }
            }

            Debug.Log("OSP Manager: Video conversion complete.");
            
            byte[] webmData = await File.ReadAllBytesAsync(tempOgv);
            
            showtape.videoData = new OspVideoData
            {
                data = Convert.ToBase64String(webmData)
            };

            File.Delete(tempOgv);
            progress?.Report(60);
        }
        else
        {
            Debug.Log("OSP Manager: Could not find video file in the directory the showtape is in, so will not convert.");
        }
    }

    private void CleanUpTempFiles(string tempWav, string tempOgg, string tempOgv)
    {
        Debug.Log("OSP Manager: Cleaning up temporary files.");
        if (tempWav != null) File.Delete(tempWav);
        if (tempOgg != null) File.Delete(tempOgg);
        if (tempOgv != null) File.Delete(tempOgv);
    }
}