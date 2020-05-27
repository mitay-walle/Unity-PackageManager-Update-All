using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager.UI;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;

namespace Plugins.Editor
{
    internal class PMUpdateAllBtnExtension : IPackageManagerExtension
    {
        public VisualElement CreateExtensionUI()
        {
            var btn = new Button(PackageManagerUpdateAllUtility.StartUpdateAll);
            btn.text = btn.name = "Update All Packages";
            return btn;
        }

        public void OnPackageSelectionChange(PackageInfo packageInfo)
        {
                
        }

        public void OnPackageAddedOrUpdated(PackageInfo packageInfo)
        {
                
        }

        public void OnPackageRemoved(PackageInfo packageInfo)
        {
                
        }
    }
    
    [InitializeOnLoad]
    public static class PackageManagerUpdateAllUtility
    {
        private const bool ADD_BTN = true;
        private const bool FULL_LOGS = false;
        
        const int MAX_ITERATIONS = 50;
        static int UpdatePackagesCurrentCount = 0;
        static bool UpdatingSomePackage;
        static bool UpdatePackagesInProgress;
        static ListRequest lRequest;
        static AddRequest aRequest;
        static readonly List<string> needToUpate = new List<string>();

        private static readonly PMUpdateAllBtnExtension extension = new PMUpdateAllBtnExtension();
        

        
        static PackageManagerUpdateAllUtility()
        {
            if (ADD_BTN)
            {
                PackageManagerExtensions.RegisterExtension(extension);
            }

//            if (FULL_LOGS) Debug.Log("check EditorPrefs");
//            UpdatePackagesInProgress = EditorPrefs.GetBool("UpdatePackagesInProgress");
//            UpdatePackagesCurrentCount  = EditorPrefs.GetInt("UpdatePackagesCurrentCount");
//
//            if (UpdatePackagesCurrentCount > MAX_ITERATIONS)
//            {
//                ForceStop();
//                Debug.LogError($"{MAX_ITERATIONS} packages updated, prevent infinite update");
//                return;
//            }
//            
//            EditorPrefs.SetInt("UpdatePackagesCurrentCount",UpdatePackagesCurrentCount+1);
//            
//            if (UpdatePackagesInProgress && !UpdatingSomePackage)
//            {
//                ContinueUpdateAllpackages();
//            }
        }

        [MenuItem("Help/Update All Packages")]
        public static void StartUpdateAll()
        {
            EditorPrefs.SetBool("UpdatePackagesInProgress", true);
            UpdatePackagesInProgress = true;
            ContinueUpdateAllpackages();
        }

        public static void ContinueUpdateAllpackages()
        {
            lRequest = Client.List(); // List packages installed for the Project
            
            EditorApplication.update += ProgressRequestPackageList;
        }

        static void ForceStop()
        {
            Debug.LogError("somethnig go wrong!");
            EditorApplication.update -= ProgressRequestPackageList;
            EditorPrefs.SetBool("UpdatePackagesInProgress", true);
            
            UpdatePackagesCurrentCount = 0;
            
            EditorPrefs.SetInt("UpdatePackagesCurrentCount",0);

            UpdatePackagesInProgress = true;
        }
        
        static void ProgressRequestPackageList()
        {
            if (lRequest == null)
            {
                ForceStop();
                return;
            }
            
            if (lRequest.IsCompleted)
            {
                if (lRequest.Status == StatusCode.Success)
                {
                    UpdateAll();
                }
                else if (lRequest.Status >= StatusCode.Failure)
                    Debug.LogError(lRequest.Error.message);

                EditorApplication.update -= ProgressRequestPackageList;
            }
        }

        private static void UpdateAll()
        {
            var check = false;

            needToUpate.Clear();
            foreach (var package in lRequest.Result)
            {
                if (package.status == PackageStatus.Available)
                {
                    if (FULL_LOGS) Debug.Log($"'{package.name}' current:{package.version } last compatible:{package.versions.latestCompatible} verified:{package.versions.verified}");
                    
                    if (package.version == package.versions.latestCompatible || package.version == package.versions.verified)
                    {
                        //Debug.Log($"{package.name} up to date");
                    }
                    else
                    {
                        if (FULL_LOGS) Debug.Log($"'{package.name}' need update from: {package.version} to: {package.versions.latestCompatible}");
                        needToUpate.Add(package.name);
                        check = true;
                    }
                }
            }

            if (check)
            {
                Debug.Log($"need to Update {needToUpate.Count} packages");
                CheckUpdateRequests();
                EditorApplication.update += CheckUpdateRequests;
            }
            else
            {
                Debug.Log($"all packages are up to date!");
            }
        }

        private static void CheckUpdateRequests()
        {
            UpdatingSomePackage = false;

            if (aRequest == null)
            {
                // update first
                if (FULL_LOGS) Debug.Log($"update first: {needToUpate[0]}");
                aRequest = Client.Add(needToUpate[0]);
                return;
            }

            if (!aRequest.IsCompleted)
            {
                UpdatingSomePackage = true;
                return;
            }

            //process update result
            if (FULL_LOGS) Debug.Log($"process update result: {aRequest.Result.name}");
            if (aRequest.Status == StatusCode.Success)
            {
                Debug.Log($"{aRequest.Result.name} updated to {aRequest.Result.version}");
            }

            if (aRequest.Status >= StatusCode.Failure)
            {
                Debug.LogError(aRequest.Error.message);
            }

            needToUpate.Remove(aRequest.Result.name);

            if (needToUpate.Count > 0)
            {
                //update next
                if (FULL_LOGS) Debug.Log($"update next: {needToUpate[0]}");
                aRequest = Client.Add(needToUpate[0]);
            }
            else
            {
                // finish
                Debug.Log($"finish updating all packages");
                EditorApplication.update -= CheckUpdateRequests;
                EditorPrefs.SetBool("UpdatePackagesInProgress", false);
                UpdatePackagesInProgress = false;
            }
        }
    }
}
