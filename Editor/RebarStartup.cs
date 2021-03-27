using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Rebar.Unity.Editor
{
    [InitializeOnLoad]
    public static class RebarStartup
    {
        private const string ASSETS_FOLDER = "Assets";
        private const string GIZMOS_FOLDER = "Gizmos";
        private const string GIZMOS_PATH = "Assets/Gizmos";
        private const string GIZMOS_PACKAGE_PATH = "Packages/com.acciaio.rebar4unity/Gizmos";
 
        static RebarStartup()
        {
            IEnumerable<string> paths = AssetDatabase.FindAssets("t:Texture2D", new [] { GIZMOS_PACKAGE_PATH })
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid));

            foreach (string path in paths)
            {
                string newPath = $"{GIZMOS_PATH}{path.Replace(GIZMOS_PACKAGE_PATH, "")}";
                if (AssetDatabase.LoadAssetAtPath<Object>(newPath) != null) continue;
                CreatePath(newPath.Substring(0, newPath.LastIndexOf('/')));
                AssetDatabase.CopyAsset(path, newPath);
            }
        }

        private static void CreatePath(string path)
        {
            string parent = "";
                foreach (string part in DeconstructPath(path))
                {
                    if (parent != "" && !AssetDatabase.IsValidFolder(part))
                        AssetDatabase.CreateFolder(parent, part.Substring(part.LastIndexOf('/') + 1));
                    parent = part;
                }
        }

        private static IEnumerable<string> DeconstructPath(string path)
        {
            string[] tokens = path.Split('/');
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for(int i = 0; i < tokens.Length; i++)
            {
                if (i != 0) builder.Append('/');
                builder.Append(tokens[i]);
                yield return builder.ToString();
            }
        } 
    }

}
