using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ShardMotion.Examples
{
    /// <summary>
    /// This script ensures, you have Animation Rigging package installed, when using this example
    /// </summary>
    [InitializeOnLoad]
    public static class ImportRigging
    {
        const string PackageId = "com.unity.animation.rigging"; // package id that need to be imported

        static ListRequest _listRequest; // request to the package manager

        /// <summary>
        /// Gets called on Editor Initialization
        /// </summary>
        static ImportRigging()
        {
            _listRequest = Client.List(true); // get all installed packages
            EditorApplication.update += CheckList;
        }

        /// <summary>
        /// Check, if the package is installed
        /// </summary>
        static void CheckList()
        {
            if (!_listRequest.IsCompleted) return;
            EditorApplication.update -= CheckList;

            if (_listRequest.Status != StatusCode.Success) return; // if the list of packages cannot be pulled, return

            // for all installed packages
            foreach (var package in _listRequest.Result)
            {
                if (package.name == PackageId) return; // if it is installed, return
            }

            // Not installed section
            
            // Dialog is drawn
            bool install = EditorUtility.DisplayDialog(
                "Animation Rigging not found",
                "You have imported Humanoid rig example, the example uses Animation Rigging package, Install now ?",
                "Install",
                "Skip"
            );

            if (install) Client.Add(PackageId); // if the user agreed to install, add package dependency. 
        }
    }
}