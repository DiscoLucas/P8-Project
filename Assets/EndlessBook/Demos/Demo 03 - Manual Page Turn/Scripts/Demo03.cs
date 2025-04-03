using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using echo17.EndlessBook;

namespace echo17.EndlessBook.Demo03
{
    public class Demo03 : MonoBehaviour
    {
        [Header("Book Settings")]
        public EndlessBook book;

        [Header("Drag Setup")]
        public Transform dragHandle; // Should be the controller tip or attach point
        public BoxCollider dragZone; // Invisible box over the book surface

        [Header("Turn Settings")]
        public float turnStopSpeed = 2f;
        public bool reversePageIfNotMidway = true;

        private bool isDragging = false;

        public void StartPageDrag()
        {
            if (book == null || book.IsTurningPages || book.IsDraggingPage)
                return;

            float normalizedTime = GetNormalizedTime();
            var direction = normalizedTime > 0.5f ? Page.TurnDirectionEnum.TurnForward : Page.TurnDirectionEnum.TurnBackward;

            book.TurnPageDragStart(direction);
            isDragging = true;
        }

        void Update()
        {
            if (!isDragging || !book.IsDraggingPage || dragHandle == null)
                return;

            float normalizedTime = GetNormalizedTime();
            book.TurnPageDrag(normalizedTime);
        }

        public void StopPageDrag()
        {
            if (!isDragging || !book.IsDraggingPage)
                return;

            float normalizedTime = GetNormalizedTime();
            bool reverse = reversePageIfNotMidway && normalizedTime < 0.5f;

            book.TurnPageDragStop(turnStopSpeed, OnPageTurnCompleted, reverse);
            isDragging = false;
        }

        private float GetNormalizedTime()
        {
            if (dragZone == null || dragHandle == null)
                return 0.5f;

            Vector3 localPoint = dragZone.transform.InverseTransformPoint(dragHandle.position);
            return Mathf.Clamp01((localPoint.x + (dragZone.size.x / 2f)) / dragZone.size.x);
        }

        private void OnPageTurnCompleted(int leftPage, int rightPage)
        {
            Debug.Log($"Page turn completed — Left: {leftPage}, Right: {rightPage}");
        }
    }
}
