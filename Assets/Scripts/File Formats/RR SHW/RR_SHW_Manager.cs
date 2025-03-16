using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using eToile;
using Global;
using UnityEngine;

/// <summary>
///     Handles all functionality related to the deprecated .*shw format
/// </summary>
public class RR_SHW_Manager : MonoBehaviour
{
    private ShowController _showController;
    public BitArray[] rshwData;

    private void Awake()
    {
        _showController = GameObject.FindGameObjectWithTag("Show Controller").GetComponent<ShowController>();
    }

    public void UpdateRrShw()
    {
        int arrayDestination = (int)(_showController.referenceAudio.time * 60);

        if (arrayDestination < rshwData.Length)
            for (int i = 0; i < 150; i++)
            {
                if (rshwData[arrayDestination].Get(i)) _showController.topDrawer[i] = true;
                if (rshwData[arrayDestination].Get(i + 150)) _showController.bottomDrawer[i] = true;
            }
    }

    public async Task LoadFromUrl(string url, IProgress<float> progress = null)
    {
        if (url != "")
        {
            progress?.Report(1);
            //Add code for opening .rshw file
            rshwFormat thefile = await Task.Run(() => rshwFormat.Read(url));

            await Task.Yield();
            progress?.Report(10);
            Debug.Log("RR .*SHW Manager: Loaded file data");

            _showController.referenceAudio.clip = OpenWavParser.ByteArrayToAudioClip(thefile.audioData);

            await Task.Yield();
            progress?.Report(20);
            Debug.Log("RR .*SHW Manager: Converted byte[] to AudioClip");

            await Task.Run(() =>
            {
                var newSignals = new List<BitArray>();
                int countlength = 0;

                if (thefile.signalData.Length > 0 && thefile.signalData[0] != 0)
                {
                    countlength = 1;
                    newSignals.Add(new BitArray(300));
                }

                float totalLength = thefile.signalData.Length;

                for (int i = 0; i < totalLength; i++)
                {
                    if (thefile.signalData[i] == 0)
                    {
                        countlength++;
                        newSignals.Add(new BitArray(300));
                    }
                    else
                    {
                        newSignals[countlength - 1].Set(thefile.signalData[i] - 1, true);
                    }

                    // only report progress every 100 iterations to avoid excessive calls
                    if (i % 100 == 0 || i == totalLength - 1)
                    {
                        float prog = 20 + 80f * (i + 1) / totalLength;
                        progress?.Report(prog);
                    }
                }

                rshwData = newSignals.ToArray();
                Debug.Log($"RR .*SHW Manager: Loaded signals of length {thefile.signalData.Length}");
            });

            //Video
            if (File.Exists(url.Remove(url.Length - 4) + "mp4"))
            {
                _showController.videoPath = '"' + url.Remove(url.Length - 4) + "mp4" + '"';
                Debug.Log($"RR .*SHW Manager: Video found at {_showController.videoPath}");
            }
            else
            {
                _showController.videoPath = "";
            }

            _showController.format = Format.RrShw;
            
            _showController.AfterLoad();

            progress?.Report(0);
        }
    }
}