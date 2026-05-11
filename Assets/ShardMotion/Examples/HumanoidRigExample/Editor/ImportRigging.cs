using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ShardMotion.Examples
{
    [InitializeOnLoad]
    public static class ImportRigging
    {
        const string PackageId = "com.unity.animation.rigging";

        static ListRequest _listRequest;

        static ImportRigging()
        {
            _listRequest = Client.List(true);
            EditorApplication.update += CheckList;
        }

        static void CheckList()
        {
            if (!_listRequest.IsCompleted) return;
            EditorApplication.update -= CheckList;

            if (_listRequest.Status != StatusCode.Success) return;

            foreach (var package in _listRequest.Result)
            {
                if (package.name == PackageId) return;
            }

            bool install = EditorUtility.DisplayDialog(
                "Animation Rigging not found",
                "You have imported Humanoid rig example, the example uses Animation Rigging package, Install now ?",
                "Install",
                "Skip"
            );

            if (install) Client.Add(PackageId);
        }
    }
}