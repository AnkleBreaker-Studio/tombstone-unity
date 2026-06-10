using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnkleBreaker.Tombstone.Editor
{
    /// <summary>
    /// Shared UI Toolkit asset loading for all Tombstone editor windows. Assets are addressed
    /// the UPM-correct way — AssetDatabase paths rooted at
    /// <c>Packages/com.anklebreaker.tombstone/Editor/Resources/Tombstone/</c> — consistently
    /// everywhere (no Resources.Load mixing). Loading is fail-soft: a missing asset logs one
    /// warning and the caller receives null / an unstyled tree instead of an exception.
    /// </summary>
    public static class TombstoneEditorUi
    {
        /// <summary>Folder holding every Tombstone editor UXML/USS asset.</summary>
        public const string UI_ROOT = "Packages/com.anklebreaker.tombstone/Editor/Resources/Tombstone/";

        /// <summary>Class applied to each window root; the theme defines its variables on it.</summary>
        public const string ROOT_CLASS = "tmb-root";

        private const string THEME_FILE = "TombstoneTheme.uss";

        /// <summary>Load a window UXML by file name (e.g. "TombstoneHub.uxml"); null when missing.</summary>
        public static VisualTreeAsset LoadTree(string fileName)
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UI_ROOT + fileName);
            if (tree == null)
                Debug.LogWarning($"[Tombstone] Missing editor UI asset: {UI_ROOT}{fileName}");
            return tree;
        }

        /// <summary>
        /// Clone a window UXML into <paramref name="root"/>, attach the shared theme, and tag
        /// the root with <see cref="ROOT_CLASS"/>. Returns false when the UXML is missing
        /// (the window then shows a plain fallback message instead of throwing).
        /// </summary>
        /// <param name="root">The window's <c>rootVisualElement</c>.</param>
        /// <param name="uxmlFileName">UXML file name inside <see cref="UI_ROOT"/>.</param>
        public static bool BuildWindow(VisualElement root, string uxmlFileName)
        {
            var tree = LoadTree(uxmlFileName);
            if (tree == null)
            {
                root.Add(new Label("Tombstone UI assets are missing. Reimport the package."));
                return false;
            }
            root.AddToClassList(ROOT_CLASS);
            var theme = AssetDatabase.LoadAssetAtPath<StyleSheet>(UI_ROOT + THEME_FILE);
            if (theme != null) root.styleSheets.Add(theme);
            tree.CloneTree(root);
            return true;
        }
    }
}
