using System.Linq;
using Object = UnityEngine.Object;

namespace UnityEditor.XR.Management
{
    internal static class BuildHelpers
    {
        internal static void CleanOldSettings<T>()
        {
            var preloadedAssets = PlayerSettings.GetPreloadedAssets();
            if (preloadedAssets == null)
                return;

            var oldSettings = from s in preloadedAssets
                where s != null && s.GetType() == typeof(T)
                select s;

            if (oldSettings != null && oldSettings.Any())
            {
                var assets = preloadedAssets.ToList();
                foreach (Object s in oldSettings) assets.Remove(s);

                PlayerSettings.SetPreloadedAssets(assets.ToArray());
            }
        }
    }
}