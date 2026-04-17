namespace nickeltin.DebugPanel.Runtime
{
    /// <summary>
    /// Handles activation of <see cref="DebugPanel"/>
    /// </summary>
    public interface IInputModule
    {
        /// <summary>
        /// Return true here to trigger panel visibility state change
        /// </summary>
        /// <returns></returns>
        bool UpdateInputModule();
    }
}


