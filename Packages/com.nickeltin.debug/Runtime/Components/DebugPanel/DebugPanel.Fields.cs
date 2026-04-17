using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace nickeltin.DebugPanel.Runtime
{
    public sealed partial class DebugPanel
    {
        [SerializeField] internal DebugPanelBaseSettings _baseSettings = DebugPanelBaseSettings.Default;
        [Tooltip("Static binding allows to define attributes in code for buttons, and they will automatically be populated in debug panel structure.")]
        [SerializeField] internal bool _useStaticBinding = true;
        [SerializeField] internal DebugPanelBindingSettings _bindingSettings = DebugPanelBindingSettings.Default;

        // [SerializeField] internal bool _customStyleSheet;
        [SerializeField] internal StyleSheet _currentStyleSheet;
        [SerializeField] internal List<StyleSheet> _additionalStyleSheets = new();
        [SerializeField] internal VisualTreeAsset _treeAsset;
        [SerializeField] internal PanelSettings _panelSettings;
        [Tooltip("If ture this game object will be removed from the scene at build time. " +
                 "That means all inner game objects will be destroyed as well, and any reference to this component break.")]
        [SerializeField] internal bool _stripFromReleaseBuild = true;

        private UIDocument _uiDocument;
        private DebugPanelLogger _logger;
        
        private VisualElement _tabsRoot;
        private VisualElement _contentRoot;
        private Stack<GroupElement> _groupsStack;
        private GroupElement _mainTabGroup;
        private GroupElement _consoleGroup;
        private GroupElement _currentGroup;

        private DebugPanelElementsFactory _debugPanelElementsFactory;
        internal IContentProvider _contentProvider;
        private ScreenOrientationState _screenOrientationState;

        private IInputModule _defaultInputModule;
        private bool _isDirty;
    }
}