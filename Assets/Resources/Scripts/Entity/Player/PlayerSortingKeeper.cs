using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

namespace Resources.Scripts.Entity.Player
{
    [DisallowMultipleComponent]
    public class PlayerSortingKeeper : MonoBehaviour
    {
        public int OriginalOrder { get; private set; }

        private Renderer _spineRenderer;
        private bool _cached;

        private readonly List<int> bottomOrders    = new List<int>();
        private readonly List<int> sideOrders      = new List<int>();
        private readonly List<int> upOrders        = new List<int>();
        private readonly List<int> treeAboveOrders = new List<int>();
        private readonly List<int> treeBelowOrders = new List<int>();

        private void Awake()
        {
            var skel = GetComponentInChildren<SkeletonAnimation>();
            if (skel == null)
            {
                Debug.LogError($"{name}: не найден SkeletonAnimation в детях");
                enabled = false;
                return;
            }

            _spineRenderer = skel.GetComponent<Renderer>();
            if (_spineRenderer == null)
            {
                Debug.LogError($"{name}: у {skel.name} нет Renderer-а");
                enabled = false;
                return;
            }

            OriginalOrder = _spineRenderer.sortingOrder;
            _cached = true;
        }

        private void ApplySorting()
        {
            if (!_cached) return;

            int newOrder;
            if (treeAboveOrders.Count > 0)
                newOrder = treeAboveOrders[treeAboveOrders.Count - 1];
            else if (treeBelowOrders.Count > 0)
                newOrder = treeBelowOrders[treeBelowOrders.Count - 1];
            else if (bottomOrders.Count > 0)
                newOrder = bottomOrders[bottomOrders.Count - 1];
            else if (sideOrders.Count > 0)
                newOrder = sideOrders[sideOrders.Count - 1];
            else if (upOrders.Count > 0)
                newOrder = upOrders[upOrders.Count - 1];
            else
                newOrder = OriginalOrder;

            _spineRenderer.sortingOrder = newOrder;
        }

        public void EnterBottom(int targetOrder)   { bottomOrders.Add(targetOrder); ApplySorting(); }
        public void ExitBottom(int targetOrder)    { bottomOrders.Remove(targetOrder); ApplySorting(); }
        public void EnterSide(int targetOrder)     { sideOrders.Add(targetOrder);   ApplySorting(); }
        public void ExitSide(int targetOrder)      { sideOrders.Remove(targetOrder);ApplySorting(); }
        public void EnterUp(int targetOrder)       { upOrders.Add(targetOrder);     ApplySorting(); }
        public void ExitUp(int targetOrder)        { upOrders.Remove(targetOrder);  ApplySorting(); }

        public void EnterTreeAbove(int targetOrder)
        {
            treeBelowOrders.Remove(targetOrder);
            treeAboveOrders.Add(targetOrder);
            ApplySorting();
        }
        public void ExitTreeAbove(int targetOrder)
        {
            treeAboveOrders.Remove(targetOrder);
            ApplySorting();
        }
        public void EnterTreeBelow(int targetOrder)
        {
            treeBelowOrders.Add(targetOrder);
            ApplySorting();
        }
        public void ExitTreeBelow(int targetOrder)
        {
            treeBelowOrders.Remove(targetOrder);
            ApplySorting();
        }
    }
}
