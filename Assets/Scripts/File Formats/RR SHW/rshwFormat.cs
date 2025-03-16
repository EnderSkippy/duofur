using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[Serializable]
[Obsolete(
    "The .*shw format is unsafe and should no longer be used. See https://aka.ms/binaryformatter for more information regarding the vulnerabilites")]
public class rshwFormat
{
    public byte[] audioData { get; set; }
    public int[] signalData { get; set; }

    public static rshwFormat Read(string filepath)
    {
        BinaryFormatter formatter = new();
        using FileStream stream = File.OpenRead(filepath);
        if (stream.Length != 0)
        {
            stream.Position = 0;
            try
            {
                return (rshwFormat)formatter.Deserialize(stream);
            }
            catch (Exception)
            {
                return null;
                throw;
            }
        }

        return null;
    }
}