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

        public bool IsTop(PotModel pot)
        {
            return CanLaunch && ReferenceEquals(_pots.Peek(), pot);
        }

        public void AppendUnlinkedPots(ICollection<PotModel> unlinkedPots)
        {
            if (unlinkedPots == null)
            {
                throw new ArgumentNullException(nameof(unlinkedPots));
            }

            var pots = _pots.ToArray();
            for (var i = 0; i < pots.Length; i++)
            {
                if (!pots[i].IsLinked)
                {
                    unlinkedPots.Add(pots[i]);
                }
            }
        }

        public void ReplaceUnlinkedPots(IReadOnlyList<PotModel> replacementPots, ref int replacementIndex)
        {
            if (replacementPots == null)
            {
                throw new ArgumentNullException(nameof(replacementPots));
            }

            var pots = _pots.ToArray();
            for (var i = 0; i < pots.Length; i++)
            {
                if (pots[i].IsLinked)
                {
                    continue;
                }

                if (replacementIndex < 0 || replacementIndex >= replacementPots.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(replacementIndex), "Replacement pot index is outside the shuffled pot list.");
                }

                pots[i] = replacementPots[replacementIndex];
                replacementIndex++;
            }

            _pots.Clear();
            for (var i = 0; i < pots.Length; i++)
            {
                _pots.Enqueue(pots[i]);
            }
        }
    }
}
