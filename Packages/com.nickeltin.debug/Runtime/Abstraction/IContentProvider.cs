namespace nickeltin.DebugPanel.Runtime
{
    public interface IContentProvider
    {
        IInputModule GetInputModule(DebugPanel panel);
        
        /// <summary>
        /// <see cref="DebugPanel"/> calls it re-build UI, put all your custom code for UI here.
        /// Use <see cref="DebugPanel.BeginGroup(GroupOptions, bool)"/>,
        /// <see cref="DebugPanel.EndGroup"/>, <see cref="DebugPanel.AddButton"/>, and others to populate content.
        /// Or helper scopes like <see cref="TabGroup"/> and <see cref="Group"/>
        /// </summary>
        void PopulateContent(DebugPanel panel);

        /// <inheritdoc cref="PopulateContent"/>
        /// <remarks>
        /// Called whenever a screen aspect ratio is more horizontal (landscape) than vertical
        /// </remarks>
        void PopulateHorizontalContent(DebugPanel panel);

        /// <inheritdoc cref="PopulateContent"/>
        /// <remarks>
        /// Called whenever a screen aspect ratio is more vertical (portrait) than horizontal
        /// </remarks>
        void PopulateVerticalContent(DebugPanel panel);
    }
}