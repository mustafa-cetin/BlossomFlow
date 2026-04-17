using System;

namespace BlossomFlow.Gameplay.Domain.Levels
{
    [Serializable]
    public struct FlowerBlockerDefinition
    {
        public FlowerBlockerDefinition(int x, int y, string lockId = null)
        {
            X = x;
            Y = y;
            LockId = lockId;
        }

        public int X;
        public int Y;
        public string LockId;
    }
}
