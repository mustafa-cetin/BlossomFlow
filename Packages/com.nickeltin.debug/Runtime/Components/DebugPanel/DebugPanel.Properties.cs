using System.Collections.Generic;
using UnityEngine.UIElements;

namespace nickeltin.DebugPanel.Runtime
{
    public sealed partial class DebugPanel
    {
        public DebugPanelElementsFactory ElementsFactory
        {
            get
            {
                if (_debugPanelElementsFactory == null)
                    _debugPanelElementsFactory = new DebugPanelElementsFactory(this);

                return _debugPanelElementsFactory;
            }
        }

        public UIDocument UIDocument => GetOrCreateComponent(ref _uiDocument);

        public DebugPanelLogger Logger => GetOrCreateComponent(ref _logger);

        public IInputModule DefaultInputModule
        {
            get
            {
                _defaultInputModule ??= new DefaultInputModule(this);
                return _defaultInputModule;
            }
        }

        public IInputModule InputModule { get; set; }

        /// <summary>
        /// Input module that is actually used can either come from <see cref="_contentProvider"/>
        /// or from <see cref="InputModule"/>
        /// or last fallback is <see cref="DefaultInputModule"/>
        /// </summary>
        public IInputModule CurrentInputModule
        {
            get
            {
                var inputModuleFromContent = _contentProvider?.GetInputModule(this);
                if (inputModuleFromContent != null)
                    return inputModuleFromContent;

                if (InputModule != null)
                    return InputModule;

                return DefaultInputModule;
            }
        }

        public bool IsVisible
        {
            get => UIDocument.enabled;
            set
            {
                var state = UIDocument.enabled;
                if (SetProperty(ref state, value))
                {
                    UIDocument.enabled = value;
                    // Re-creating UI in case of enabling UIDocument since previous UI hierarchy most likely will be missing after enable
                    if (value)
                    {
                        SetDirty();
                        PanelOpened?.Invoke(this);
                    }
                    else
                    {
                        PanelClosed?.Invoke(this);
                    }
                }
            }
        }

        public DebugPanelBaseSettings BaseSettings
        {
            get => _baseSettings;
            set => SetProperty(ref _baseSettings, value);
        }

        public bool UseStaticBinding
        {
            get => _useStaticBinding;
            set
            {
                _useStaticBinding = value;
                _isDirty = true;
            }
        }

        public DebugPanelBindingSettings BindingSettings
        {
            get => _bindingSettings;
            set => SetProperty(ref _bindingSettings, value);
        }

        public bool StripFromReleaseBuild
        {
            get => _stripFromReleaseBuild;
            set => _stripFromReleaseBuild = value;
        }

        public StyleSheet CurrentStyleSheet
        {
            get => _currentStyleSheet;
            set => SetProperty(ref _currentStyleSheet, value);
        }

        public VisualTreeAsset TreeAsset
        {
            get => _treeAsset;
            set => SetProperty(ref _treeAsset, value);
        }

        public PanelSettings PanelSettings
        {
            get => _panelSettings;
            set => SetProperty(ref _panelSettings, value);
        }

        public IReadOnlyList<StyleSheet> AdditionalStyleSheets => _additionalStyleSheets;

        public void AddAdditionalStyleSheet(StyleSheet sheet)
        {
            _additionalStyleSheets.Add(sheet);
            SetDirty();
        }

        public void RemoveAdditionalStyleSheet(StyleSheet sheet)
        {
            if (_additionalStyleSheets.Remove(sheet)) SetDirty();
        }

        public IEnumerable<StyleSheet> GetAllStyleSheets()
        {
            if (CurrentStyleSheet != null) yield return CurrentStyleSheet;
            foreach (var sheet in AdditionalStyleSheets)
                if (sheet != null) yield return sheet;
        }
    }
}