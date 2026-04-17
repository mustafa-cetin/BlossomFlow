using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using nickeltin.DebugPanel.Runtime.Binding;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace nickeltin.DebugPanel.Runtime
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument), typeof(DebugPanelLogger))]
    public sealed partial class DebugPanel : MonoBehaviour
    {
        private readonly struct ScreenOrientationState
        {
            public readonly bool IsHorizontal;
            public readonly bool IsVertical;

            public ScreenOrientationState(bool isHorizontal, bool isVertical)
            {
                IsHorizontal = isHorizontal;
                IsVertical = isVertical;
            }
            
            public static ScreenOrientationState Capture()
            {
                float width = Screen.width;
                float height = Screen.height;

                var isLandscape = width > height;
                var isPortrait = height > width;
                
                if (Mathf.Approximately(width, height))
                {
                    isLandscape = false;
                    isPortrait = false;
                }

                return new ScreenOrientationState(isLandscape, isPortrait);
            }
        }
        
        public static event Action<DebugPanel> PanelClosed; 
        public static event Action<DebugPanel> PanelOpened; 
        
        private void Start()
        {
            if (Application.isPlaying)
            {
                Refresh();
                SelectTabWithNameAtStart();
                if (_baseSettings.HideAtStart)
                {
                    IsVisible = false;
                }
            }
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Update()
        {
            if (_isDirty)
            {
                Refresh();
                _isDirty = false;
            }
            
            if (Application.isPlaying)
            {
                if (CurrentInputModule.UpdateInputModule())
                {
                    ToggleVisibility();
                }
            }
            
            var newStateOrientation = ScreenOrientationState.Capture();
            
            if (!EqualityComparer<ScreenOrientationState>.Default.Equals(_screenOrientationState, newStateOrientation))
            {
                _screenOrientationState = newStateOrientation;
                _isDirty = true;
            }
        }
        
        [ContextMenu("Refresh")]
        private void Refresh()
        {
            _contentProvider = GetComponent<IContentProvider>();
            _groupsStack = new Stack<GroupElement>();
            _mainTabGroup = ElementsFactory.CreateGroupElement(GroupOptions.Vertical, true);

            if (_contentProvider == null)
            {
                _contentProvider = BuiltInContentProvider.EmptyImplementation.Instance;
                Debug.LogError($"No content provider found for a panel, using default one (empty). " +
                               $"Make sure you attached {nameof(ContentProvider)} to your {nameof(DebugPanel)}", this);
            }
            
#if UNITY_EDITOR
            ValidatePanelSettings?.Invoke(this);
#endif
           
            if (TreeAsset == null)
            {
                UIDocument.visualTreeAsset = null;
                return;
            }
            
            UIDocument.visualTreeAsset = TreeAsset;
            UIDocument.panelSettings = PanelSettings;
            
            
            foreach (var styleSheet in GetAllStyleSheets())
            {
                UIDocument.rootVisualElement.styleSheets.Add(styleSheet);
            }
            
            if (!Q("ContentContainer", out _contentRoot))
            {
                return;
            }
            
            _contentRoot.Clear();
            _contentRoot.Add(_mainTabGroup);
            _currentGroup = _mainTabGroup;


            if(!Q("TabsContainer", out _tabsRoot))
            {
                return;
            }

            _tabsRoot.Clear();
            
            AddTab_Internal(_baseSettings.MainTabName, _mainTabGroup);

            PopulateContent();
            

            if (Application.isPlaying)
            {
                Logger.enabled = _baseSettings.UseConsoleTab;
                if (gameObject.activeInHierarchy)
                {
                    FocusOnFirstInteractableElementDelayed();
                }
                else
                {
                    FocusOnFirstInteractableElement();
                }
            }
            
            if (_baseSettings.UseConsoleTab)
            {
                const string consoleName = "CONSOLE";

                _consoleGroup = ElementsFactory.CreateGroupElement(GroupOptions.Vertical);
                _consoleGroup.name = consoleName;
                
                _contentRoot.Add(_consoleGroup);
                AddTab_Internal(consoleName, _consoleGroup);

                var console = new ConsoleElement(Logger);
                _consoleGroup.Tab.onSelect += () =>
                {
                    StartCoroutine(WaitForFrame(() => console.ScrollToBottom()));
                };
                _consoleGroup.AddContent(console);
            }

            ValidateGroupsClosing();

            EnsureStaticBindings();
            
            TabElement.Initialize(_tabsRoot);
            
            SelectTabWithNameAtStart();
        }
        
        private void FocusOnFirstInteractableElement()
        {
            var firstFocusable = _mainTabGroup.Query().Where(element => element.canGrabFocus).First();
            if (firstFocusable != null)
            {
                firstFocusable.Focus();
            } 
        }

        private void FocusOnFirstInteractableElementDelayed()
        {
            // Debug.Log("FocusOnFirstInteractableElementDelayed");
            StartCoroutine(WaitForFrame(FocusOnFirstInteractableElement));
        }

        private void PopulateContent()
        {
            if (_screenOrientationState.IsVertical)
                _contentProvider.PopulateVerticalContent(this);
            else if (_screenOrientationState.IsHorizontal)
                _contentProvider.PopulateHorizontalContent(this);
            else
                _contentProvider.PopulateContent(this);
        }
        
        private void SelectTabWithNameAtStart()
        {
            if (_baseSettings.SelectTabWithNameAtStart)
            {
                TabElement.FirstOrDefault(_tabsRoot, _baseSettings.SelectTabWithNameAtStart)?.Select();
            }
        }
        private static IEnumerator WaitForFrame(Action action)
        {
            yield return null;
            action();
        }
        
        private void ValidateGroupsClosing()
        {
            if (_groupsStack.Count > 0)
            {
                Debug.LogError($"{nameof(EndGroup)} called less times than {nameof(BeginGroup)}");
            }

            if (_currentGroup != _mainTabGroup)
            {
                Debug.LogError($"Tab group were not closed with {nameof(EndTabGroup)}");
            }
        }

        private bool Q(string elementName, out VisualElement element)
        {
            element = UIDocument.rootVisualElement.Q(elementName);
            if (element != null) return true;
            Debug.LogError($"Query element with name {elementName} not found");
            return false;
        }

        public void ToggleVisibility() => IsVisible = !IsVisible;

        public GroupElement GetCurrentGroup() => _groupsStack.Count > 0 ? _groupsStack.Peek() : _currentGroup;
        

        #region Static Binding

        private void EnsureStaticBindings()
        {
            if (!_useStaticBinding)
                return;


            if (!DebugPanelStaticBindingUtil.IsBindingsReady)
                return;

            var bindings = DebugPanelStaticBindingUtil.GetBindingsForPanel(_bindingSettings.BindingName);

            foreach (var binding in bindings)
            {
                EnsureBinding(binding);
            }
        }

        private void EnsureBinding(DebugPanelStaticBindingUtil.StaticBoundButton bindedButton)
        {
            bool Compare(string a, string b) => string.Compare(a, b, StringComparison.OrdinalIgnoreCase) == 0;
            
            var attribute = bindedButton.Attribute;
            var canCreateGroups = attribute.AllowToCreateGroups && _bindingSettings.AllowToCreateNewGroups;

            var path = ListPool<string>.Get();
            path.AddRange(attribute.Path);
            
            var tabs = ListPool<TabElement>.Get();
            tabs.AddRange(TabElement.GetTabs(_tabsRoot));

            var tabName = "Tab" + attribute.Tab;
            var tab = tabs.FirstOrDefault(t => Compare(t.name, "Tab"+attribute.Tab));

            if (tab == null) tab = tabs.FirstOrDefault();

            var lastGroup = GroupElement.GetFromTab(tab, _contentRoot) ?? _mainTabGroup;

            for (var i = 0; i < path.Count - 1; i++)
            {
                var element = path[i];
                var group = lastGroup.Q<GroupElement>("Group" + element);
                if (group == null)
                {
                    if (canCreateGroups)
                    {
                        var options = GroupOptions.None;
                        if (attribute.PreferHorizontalGroups)
                        {
                            options |= GroupOptions.Horizontal;
                        }
                        group = ElementsFactory.CreateGroupElement(options, element);
                        lastGroup.AddContent(group);
                    }
                    else
                    {
                        Debug.LogError($"Missing hierarchy for {tabName}/{string.Join('/',attribute.Path)}. " +
                                       $"You might want to enable {nameof(DebugPanelBindingSettings.AllowToCreateNewGroups)}");
                        return;
                    }
                }
                lastGroup = group;
            }

            var buttonName = path.LastOrDefault();
            if (string.IsNullOrEmpty(buttonName)) buttonName = bindedButton.Info.Name;
            var button = ElementsFactory.CreateButton(buttonName, bindedButton.Invoke);
            lastGroup.AddContent(button);

            ListPool<string>.Release(path);
            ListPool<TabElement>.Release(tabs);
        }

        #endregion


        [ContextMenu("SetDirty")]
        public void SetDirty() => _isDirty = true;

        private bool SetProperty<T>(ref T property, T value)
        {
            if (EqualityComparer<T>.Default.Equals(property, value)) return false;

            SetDirty();
            property = value;
            return true;
        }

        private T GetOrCreateComponent<T>(ref T component) where T : Component
        {
            if (component == null)
                component = GetComponent<T>();
            if (component == null) 
                component = gameObject.AddComponent<T>();
            return component;
        }
    }
}