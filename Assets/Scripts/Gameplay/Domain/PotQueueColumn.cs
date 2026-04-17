using System;
using System.Collections.Generic;

namespace BlossomFlow.Gameplay.Domain
{
    public sealed class PotQueueColumn
    {
        private readonly Queue<PotModel> _pots;

        public PotQueueColumn(IEnumerable<PotModel> pots)
        {
            _pots = new Queue<PotModel>(pots ?? throw new ArgumentNullException(nameof(pots)));
        }

        public int Count => _pots.Count;
        public bool CanLaunch => _pots.Count > 0;
        public IReadOnlyList<PotModel> Pots => _pots.ToArray();

        public PotModel PeekTop()
        {
            if (!CanLaunch)
            {
                throw new InvalidOperationException("Cannot peek an empty launch column.");
            }

            return _pots.Peek();
        }

        public PotModel LaunchTop()
        {
            if (!CanLaunch)
            {
                throw new InvalidOperationException("Cannot launch from an empty launch column.");
            }

            var pot = _pots.Dequeue();
            pot.Launch();
            return pot;
        }
    }
}
