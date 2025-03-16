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
        string tempMp3Path = null;
        
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

            tempMp3Path = Path.GetTempFileName();
            Debug.Log($"OSP Manager: Created temporary mp3 file at {tempMp3Path}");
            await File.WriteAllBytesAsync(tempMp3Path, audioData);
            
            progress?.Report(70);

            AudioClip audioClip = await UriToAudioClipAsync(tempMp3Path);
            
            if (!audioClip) throw new Exception("OSP Manager: Failed to create audio clip");
            
            _showController.referenceAudio.clip = audioClip;
            Debug.Log("OSP Manager: Converted audio to AudioClip & set referenceAudio.clip");

            _showController.format = Format.OpenShowtapePackage;
            
            _showController.AfterLoad();
            progress?.Report(0);
            
            File.Delete(tempMp3Path);
        }
        catch (Exception e)
        {
            Debug.LogError("OSP Manager: Failed to load showtape\n" + e.Message + "\n" + e.StackTrace);
            if (tempMp3Path != null) File.Delete(tempMp3Path);
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

        using (var uwr = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.MPEG))
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
        string tempMp3 = null;
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
                    Debug.Log($"OSP Manager: Created temporary WAV container @ {tempWav}");

                    tempMp3 = Path.GetTempFileName() + ".mp3";
                    Debug.Log($"OSP Manager: Created temporary MP3 container @ {tempMp3}");

                    await File.WriteAllBytesAsync(tempWav, shw.audioData);
                    Debug.Log(
                        $"OSP Manager: Wrote .*shw WAV audio data of length {shw.audioData.Length} to temporary WAV container");

                    progress?.Report(15);
                    
                    ProcessStartInfo processStartInfo = new()
                    {
                        FileName = _ffmpeg,
                        Arguments = $"-i \"{tempWav}\" -vn -b:a 128k \"{tempMp3}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    int numChannels = 0;
                    int sampleRate = 0;
                    int bitRate = 0;

                    using (Process process = Process.Start(processStartInfo))
                    {
                        if (process != null)
                        {
                            string standardError = await process.StandardError.ReadToEndAsync();
                            string standardOutput = await process.StandardOutput.ReadToEndAsync();
        
                            process.WaitForExit();

                            if (process.ExitCode != 0)
                            {
                                Debug.LogError($"FFmpeg exited with error code {process.ExitCode}\n{standardError}");
                                File.Delete(tempWav);
                                File.Delete(tempMp3);
                                progress?.Report(0);
                                return null;
                            }

                            // Regex patterns to extract channels, sample rate, and bit rate
                            var channelsRegex = new System.Text.RegularExpressions.Regex(@"Audio:.*?(\d+) channels");
                            var sampleRateRegex = new System.Text.RegularExpressions.Regex(@"(\d+) Hz");
                            var bitRateRegex = new System.Text.RegularExpressions.Regex(@"bitrate: (\d+) kb/s");

                            // Extract the number of channels
                            var channelsMatch = channelsRegex.Match(standardError);
                            if (channelsMatch.Success)
                            {
                                numChannels = int.Parse(channelsMatch.Groups[1].Value);
                            }

                            // Extract the sample rate
                            var sampleRateMatch = sampleRateRegex.Match(standardError);
                            if (sampleRateMatch.Success)
                            {
                                sampleRate = int.Parse(sampleRateMatch.Groups[1].Value);
                            }

                            // Extract the bit rate
                            var bitRateMatch = bitRateRegex.Match(standardError);
                            if (bitRateMatch.Success)
                            {
                                bitRate = int.Parse(bitRateMatch.Groups[1].Value);
                            }
                        }
                    }

                    progress?.Report(30);

                    Debug.Log("OSP Manager: Converted WAV container to MP3 via ffmpeg");

                    byte[] tempMp3Bytes = await File.ReadAllBytesAsync(tempMp3);

                    progress?.Report(35);

                    // Set the audio data, channels, sample rate, and bit rate
                    showtape.audioData = new OspAudioData
                    {
                        data = Convert.ToBase64String(tempMp3Bytes),
                        channels = numChannels,
                        sampleRate = sampleRate,
                        bitRate = bitRate
                    };


                    progress?.Report(40);

                    Debug.Log("OSP Manager: Audio conversion to MP3 complete. Deleting temporary files");
                    File.Delete(tempMp3);
                    File.Delete(tempWav);

                    Debug.Log("OSP Manager: Converting signals to frames");

                    // Process signals with progress update
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

                            if (progress != null && i >= totalSignals * 40 && i % (totalSignals / 100) == 0)
                            {
                                float percentage = 40 + (float)(i - totalSignals * 40) / (totalSignals * 60) * 60;
                                progress.Report(percentage);
                            }
                        }

                        return newSignals.Select(lst => lst.ToArray()).ToArray();
                    });

                    Debug.Log("OSP Manager: Signal to frame conversion complete.");

                    Debug.Log($"OSP Manager: {ex} to OSP conversion complete!");
                    return showtape;
                }

                return null;
            }

            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"OSP Manager: Conversion failed with exception:\n{e}");
            if (tempWav != null)
                File.Delete(tempWav);

            if (tempMp3 != null)
                File.Delete(tempMp3);
            
            progress?.Report(0);

            return null;
        }
    }
}