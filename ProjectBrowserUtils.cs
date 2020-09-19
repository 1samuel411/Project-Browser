using Sisus.OdinSerializer.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace EditorExtensions
{
    [InitializeOnLoad]
    public static class ProjectBrowserUtils
    {

        // ******************** Private Variables *************************
        private static Assembly editorAssembly;
        private static Type projectBrowserType;
        private static Type searchFilterType;
        private static Type treeViewController;
        private static MethodInfo showFolderContentsMethod;
        private static PropertyInfo isLocked; // Whether the project window is locked or not

        private static FieldInfo m_ViewModeField;    // Get the view mode (One column vs two columns)

        private static PropertyInfo m_TreeViewController_HorizontalScrollbar;    // The GUI Style for the Horiz Scrollbar
        private static PropertyInfo m_TreeViewController_VerticalScrollbar;    // The GUI Style for the Vert Scrollbar
        private static FieldInfo m_FolderTree;    // The Folder Tree is the left column in two column layout

        private static FieldInfo m_DirectoriesAreaWidthField;    // The width of the Folder Tree
        private static FieldInfo k_MinDirectoriesAreaWidth;    // The minimum width for the Folder Tree

        private static FieldInfo m_SelectedPathField;    // The path that is displayed at the bottom of the window. Doesn't modify anything, just changes what's displayed at the bottom of the window

        private static FieldInfo m_Breadcrumbs;    // The breadcrumbs controller for the window

        // The selected search type. The options are listed in the header when searching. Options between, All, InPackages, InAssets, InFolder, AssetStore
        private static FieldInfo m_SearchArea;   // The area the search filter will search inside. 
        private static FieldInfo m_SearchFilter; // The search filter the project window is using

        private static FieldInfo m_SearchAllAssets;  // The GUI displayed for 'All' while searching
        private static FieldInfo m_SearchAssetStore; // The GUI displayed for 'Asset Store' while searching. Is auto replaced.
        private static FieldInfo m_SearchInAssetsOnly; // The GUI displayed for 'Search In Assets' while searching
        private static FieldInfo m_SearchInFolders; // The GUI displayed for 'Search In Folder' while searching
        private static FieldInfo m_SearchInPackagesOnly; // The GUI displayed for 'In Packages Only' while searching

        private static PropertyInfo listAreaGridSize; // The grid size slider for two column view
        private static FieldInfo startListAreaGridSize; // The grid size slider for two column view. Change this before initializing to set the grid size

        private static MethodInfo initMethod;    // The method that the project window calls when being made that initialize it internally

        // *********************** Private Methods ************************
        // Called when unity
        static ProjectBrowserUtils()
        {
            Initialize();
        }

        // Get all the assembly types and fields we need built into Unity
        private static void Initialize()
        {
            editorAssembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
            // The packaged project browser can't be accessed so we need to use <i>Reflection</i> to access it (The italics for coolness)
            projectBrowserType = editorAssembly.GetType("UnityEditor.ProjectBrowser");
            searchFilterType = editorAssembly.GetType("UnityEditor.SearchFilter");
            treeViewController = editorAssembly.GetType("UnityEditor.IMGUI.Controls.TreeViewController");

            m_SearchArea = searchFilterType.GetField("m_SearchArea", BindingFlags.NonPublic | BindingFlags.Instance);
            m_SearchFilter = projectBrowserType.GetField("m_SearchFilter", BindingFlags.NonPublic | BindingFlags.Instance);

            m_SearchAllAssets = projectBrowserType.GetField("m_SearchAllAssets");
            m_SearchAssetStore = projectBrowserType.GetField("m_SearchAssetStore");
            m_SearchInAssetsOnly = projectBrowserType.GetField("m_SearchInAssetsOnly");
            m_SearchInFolders = projectBrowserType.GetField("m_SearchInFolders");
            m_SearchInPackagesOnly = projectBrowserType.GetField("m_SearchInPackagesOnly");

            m_TreeViewController_HorizontalScrollbar = treeViewController.GetProperty("horizontalScrollbarStyle");
            m_TreeViewController_VerticalScrollbar = treeViewController.GetProperty("verticalScrollbarStyle");

            m_ViewModeField = projectBrowserType.GetField("m_ViewMode", BindingFlags.NonPublic | BindingFlags.Instance);
            m_Breadcrumbs = projectBrowserType.GetField("m_BreadCrumbs", BindingFlags.NonPublic | BindingFlags.Instance);
            m_FolderTree = projectBrowserType.GetField("m_FolderTree", BindingFlags.NonPublic | BindingFlags.Instance);
            
            m_DirectoriesAreaWidthField = projectBrowserType.GetField("m_DirectoriesAreaWidth", BindingFlags.NonPublic | BindingFlags.Instance);
            k_MinDirectoriesAreaWidth = projectBrowserType.GetField("k_MinDirectoriesAreaWidth", BindingFlags.NonPublic | BindingFlags.Instance);
            
            m_SelectedPathField = projectBrowserType.GetField("m_SelectedPath", BindingFlags.NonPublic | BindingFlags.Instance);

            listAreaGridSize = projectBrowserType.GetProperty("listAreaGridSize");
            startListAreaGridSize = projectBrowserType.GetField("m_StartGridSize", BindingFlags.NonPublic | BindingFlags.Instance);

            isLocked = projectBrowserType.GetProperty("isLocked", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetField);
            
            initMethod = projectBrowserType.GetMethod("Init", BindingFlags.Instance | BindingFlags.Public);
            showFolderContentsMethod = projectBrowserType.GetMethod("ShowFolderContents", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        // ************************* Public Methods ***********************
        // Creates a Project Window with the set parameters
        public static EditorWindow CreateProjectWindow(ScriptableObject mb, string name, int gridSize)
        {
            MethodInfo[] methods = typeof(EditorWindow).GetMethods().Where(x => x.Name == nameof(EditorWindow.CreateWindow)).ToArray();
            MethodInfo method = methods.FirstOrDefault(x => x.GetParameters().Length == 2);
            MethodInfo generic = method.MakeGenericMethod(projectBrowserType);
            EditorWindow window = generic.Invoke(mb, new object[] { name, new Type[0] }) as EditorWindow;
            startListAreaGridSize.SetValue(window, gridSize);

            window.ShowTab();
            
            initMethod.Invoke(window, null);
            window.Repaint();

            return window;
        }

        // Lock the project window (top right lock button)
        public static void SetLocked(EditorWindow window, bool val)
        {
            isLocked.SetValue(window, val);
        }

        // Open up to a path
        public static void ViewFolder(EditorWindow window, string path)
        {
            showFolderContentsMethod.Invoke(window, new object[] { AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object)).GetInstanceID(), false });
        }

        // Sets the window to One Column Layout
        public static void SetOneColumn(EditorWindow window)
        {
            ProjectBrowserUtils.m_ViewModeField.SetValue(window, 0);
        }

        // Sets the window to Two Column Layout
        public static void SetTwoColumn(EditorWindow window)
        {
            ProjectBrowserUtils.m_ViewModeField.SetValue(window, 1);
        }

        // Clears out the breadcrumbs for that path if it exists.
        public static void IgnoreBreadcrumbsPath(EditorWindow window, string path)
        {
            object crumbVal = m_Breadcrumbs.GetValue(window);
            List<KeyValuePair<GUIContent, string>> crumbsList = crumbVal as List<KeyValuePair<GUIContent, string>>;
            string[] pathSplit = path.Replace('\\', '/').Split('/');
            if(crumbsList != null)
            {
                for(int i = 0; i < pathSplit.Length; i++)
                {
                    if (crumbsList.Count <= 0)
                        break;
                    string[] crumbSplit = crumbsList[0].Value.Split('/');
                    if (crumbSplit[crumbSplit.Length - 1] == pathSplit[i])
                        crumbsList.RemoveAt(0);
                    else
                        break;
                }
                m_Breadcrumbs.SetValue(window, crumbsList);
            }
        }

        // Hides the Folder Tree view by setting the width to 0.
        // Note: You must also clear the scrollbars so that you dont get artifacts of scrollbars for the Folder Tree.
        public static void HideFolderTree(EditorWindow window)
        {
            k_MinDirectoriesAreaWidth.SetValue(window, 0);
            m_DirectoriesAreaWidthField.SetValue(window, 0);
        }

        // Removes the scrollbars from the tree view
        public static void ClearFolderViewScrollbars(EditorWindow window)
        {
            var folderTree = m_FolderTree.GetValue(window);
            m_TreeViewController_HorizontalScrollbar.SetValue(folderTree, new GUIStyle());
            m_TreeViewController_VerticalScrollbar.SetValue(folderTree, new GUIStyle());
        }

        // Sets the footer text.
        // Note: Updated when the selection changes and overriden
        public static void SetSelectedPathText(EditorWindow window, string text)
        {
            m_SelectedPathField.SetValue(window, text);
        }

        // Returns the grid size (Set in two column layout)
        public static int GetListAreaGridSize(EditorWindow window)
        {
            var val = ProjectBrowserUtils.listAreaGridSize.GetValue(window);
            return int.Parse(val.ToString());
        }

        // Sets the grid size (Set in two column layout)
        public static void SetSearchViewState(EditorWindow window, int state)
        {
            var filter = m_SearchFilter.GetValue(window);
            m_SearchArea.SetValue(filter, (int)state);
        }

        // Returns the enum index of the search view state. (Search within the folder, assets, etc).
        public static int GetSearchViewState(EditorWindow window)
        {
            int val = (int)m_SearchArea.GetValue(m_SearchFilter.GetValue(window));
            return val;
        }

        // Removes all the GUI displaying the options for the other search view states
        public static void ShowFolderSearchOnly(EditorWindow window)
        {
            m_SearchAllAssets.SetValue(window, new GUIContent());
            m_SearchAssetStore.SetValue(window, new GUIContent());
            m_SearchInAssetsOnly.SetValue(window, new GUIContent());
            m_SearchInPackagesOnly.SetValue(window, new GUIContent());
        }

        public static void GetOrCreateAssetFolder(string path, bool ping)
        {
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (obj == null)
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
                if(ping)
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
            }
        }
    }
}
