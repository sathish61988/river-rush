using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RiverRush.UI
{
    /// <summary>
    /// Provides responsive visual and tactile feedback for mobile buttons.
    /// Scales down on press and bounces back on release.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Animation Settings")]
        [SerializeField] private float pressedScale = 0.94f;
        [SerializeField] private float animationDuration = 0.08f;
        [SerializeField] private bool enableHover = true;

        private Vector3 originalScale;
        private Coroutine activeScaleRoutine;
        private Button button;

        private void Awake()
        {
            originalScale = transform.localScale;
            button = GetComponent<Button>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button != null && !button.interactable) return;
            AnimateScale(originalScale * pressedScale);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (button != null && !button.interactable) return;
            AnimateScale(originalScale);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!enableHover || (button != null && !button.interactable)) return;
            AnimateScale(originalScale * 1.03f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            AnimateScale(originalScale);
        }

        private void AnimateScale(Vector3 targetScale)
        {
            if (activeScaleRoutine != null)
            {
                StopCoroutine(activeScaleRoutine);
            }
            if (gameObject.activeInHierarchy)
            {
                activeScaleRoutine = StartCoroutine(ScaleRoutine(targetScale));
            }
            else
            {
                transform.localScale = targetScale;
            }
        }

        private IEnumerator ScaleRoutine(Vector3 target)
        {
            Vector3 start = transform.localScale;
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(start, target, elapsed / animationDuration);
                yield return null;
            }
            transform.localScale = target;
            activeScaleRoutine = null;
        }

        private void OnDisable()
        {
            transform.localScale = originalScale;
        }
    }
}
