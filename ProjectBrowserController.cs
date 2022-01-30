using Ludiq.OdinSerializer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EditorExtensions
{
    [FilePath("ProjectBrowser/projectbrowserstate.dat", FilePathAttribute.Location.PreferencesFolder)]
    public class ProjectBrowserController : ScriptableSingleton<ProjectBrowserController>
    {
        
        [Serializable]
        public class ProjectWindowData
        {
            public EditorWindow window;
            public bool showing;
            public string name;
            public string path;
            [OdinSerialize]
            public Rect position;
            public int gridScale;
        }

        [SerializeField]
        public List<ProjectWindowData> projectWindows = new List<ProjectWindowData>();

        private void OnEnable()
        {
            EditorApplication.update += UpdateLoop;

            projectWindows.RemoveAll(x => x == null);

            projectWindows.ForEach(projectBrowser =>
            {
                if(projectBrowser.showing == false)
                { 
                    if(projectBrowser.window != null)
                        projectBrowser.window.Close();
                }
                else
                {
                    if (projectBrowser.window == null)
                        CreateProjectBrowser(projectBrowser.path, projectBrowser.name);
                }
            });
        }
        private void OnDisable()
        {
            EditorApplication.update -= UpdateLoop;

            projectWindows.ForEach(projectBrowser =>
            {
                if (projectBrowser != null)
                {
                    projectBrowser.showing = false;
                    projectBrowser.window.Close();
                }
            });
        }

        private void UpdateLoop()
        {
            projectWindows.ForEach(projectBrowser =>
            {
                if (projectBrowser.showing)
                {
                    if (projectBrowser.window == null)
                    {
                        projectBrowser.showing = false;
                    }
                    else
                    {
                        if (projectBrowser.window.titleContent.text != projectBrowser.name)
                        {
                            projectBrowser.window.Close();
                            projectBrowser.showing = false;
                            return;
                        }
                        // Save position and grid size
                        projectBrowser.position = projectBrowser.window.position;
                        var val = ProjectBrowserUtils.GetListAreaGridSize(projectBrowser.window);

                        ProjectBrowserUtils.HideFolderTree(projectBrowser.window);
                        ProjectBrowserUtils.SetSearchViewState(projectBrowser.window, 3);
                        ProjectBrowserUtils.SetSelectedPathText(projectBrowser.window, "Viewing Browser: " + projectBrowser.path);
                        ProjectBrowserUtils.IgnoreBreadcrumbsPath(projectBrowser.window, Directory.GetParent(projectBrowser.path).ToString());
                        ProjectBrowserUtils.ShowFolderSearchOnly(projectBrowser.window);

                        projectBrowser.window.Repaint();
                    }
                }
            });
        }

        public void CreateProjectBrowser(string path, string name)
        {
            if (projectWindows.Exists(x => x.name == name) == false)
            {
                ProjectWindowData newDat = new ProjectWindowData();
                newDat.name = name;
                newDat.path = path;
                newDat.showing = false;
                newDat.gridScale = 70;
                newDat.position = new Rect(UnityEngine.Random.Range(0, Screen.width), UnityEngine.Random.Range(0, Screen.height), 600, 300);
                projectWindows.Add(newDat);
            }
            ProjectWindowData projectBrowser = projectWindows.First(x => x.name == name);
            EditorWindow windowToClose = null;
            if (projectBrowser.window != null)
            {
                projectBrowser.showing = false;
                windowToClose = projectBrowser.window;
                projectBrowser = null;
            }
            // A new folder needs to be created if none exist
            ProjectBrowserUtils.GetOrCreateAssetFolder(path, true);

            // Set the path to saved
            projectBrowser.path = path;

            projectBrowser.window = ProjectBrowserUtils.CreateProjectWindow(this, name, projectBrowser.gridScale);

            ProjectBrowserUtils.SetTwoColumn(projectBrowser.window);
            ProjectBrowserUtils.SetLocked(projectBrowser.window, true);
            ProjectBrowserUtils.ViewFolder(projectBrowser.window, path);
            ProjectBrowserUtils.ClearFolderViewScrollbars(projectBrowser.window);
            ProjectBrowserUtils.ShowFolderSearchOnly(projectBrowser.window);

            // Move to saved spot
            if (projectBrowser.position == new Rect())
            {
                projectBrowser.position = projectBrowser.window.position;
            }
            projectBrowser.window.position = projectBrowser.position;

            projectBrowser.showing = true;

            if (windowToClose != null)
            {
                windowToClose.Close();
            }
        }
        [MenuItem("Assets/Open In New Window", false)]
        public static void OpenInNewWindow()
        {
            var gs = Selection.objects;
            string[] assetPath = gs.Select(AssetDatabase.GetAssetPath).ToArray();
            for (var index = 0; index < assetPath.Length; index++) 
            {
                var a = assetPath[index];
                if (!Directory.Exists(a))
                    a = Path.GetDirectoryName(a);
                instance.CreateProjectBrowser(a, Path.GetFileName(a));
            }
        }
    }
}
