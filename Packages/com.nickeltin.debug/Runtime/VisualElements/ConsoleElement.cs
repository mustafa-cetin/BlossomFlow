using System.Collections;
using System.Collections.Generic;
using nickeltin.InternalBridge.Runtime;
using UnityEngine.UIElements;

namespace nickeltin.DebugPanel.Runtime
{
    public class ConsoleElement : VisualElement
    {
        private readonly DebugPanelLogger _logger;
        private readonly ListView _listView;

        public ConsoleElement(DebugPanelLogger logger)
        {
            AddToClassList(DebugPanelStyleClasses.console);
            var clearButton = new Button(ClearButtonClick)
            {
                name = "ClearButton",
                text = "Clear"
            };

            _listView = new ListView(GetContents(logger), -1, CreateElement, BindElement)
            {
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
            };
            
            _listView.selectionChanged += OnSelectionChanged;
            
            var scrollView = _listView.GetScrollView();
            DebugPanelElementsFactory.SetupScrollView(scrollView);

            Add(_listView);
            Add(clearButton);

            _logger = logger;
            _logger.onLogUpdate += UpdateLogText;
            
            UpdateLogText();
        }

        private void OnSelectionChanged(IEnumerable<object> selectedItems)
        {
            // When an item is selected (checked), also give it actual focus
            if (_listView.selectedIndex < 0) return;
            var selectedElement = _listView.GetRootElementForIndex(_listView.selectedIndex);
            selectedElement?.Focus();
        }

        private IList GetContents(DebugPanelLogger logger) => logger.LogsSource;

        private VisualElement CreateElement() => new ConsoleLogElement();

        private void BindElement(VisualElement element, int i) => ((ConsoleLogElement)element).SetLog(_logger.Logs[i]);

        private void ClearButtonClick() => _logger.Clear();

        public void ScrollToBottom() => _listView.ScrollToItem(-1);

        private void UpdateLogText() => _listView.RefreshItems();
    }
}