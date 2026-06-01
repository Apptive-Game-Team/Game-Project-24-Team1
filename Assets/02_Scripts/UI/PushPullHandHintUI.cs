using MushOut.Interactables;
using MushOut.Player;
using UnityEngine;

namespace MushOut.UI
{
    public class PushPullHandHintUI : MonoBehaviour
    {
        [Header("Sprites")]
        [SerializeField] private Sprite availableHandSprite;
        [SerializeField] private Sprite grabbedHandSprite;

        [Header("Detection")]
        [SerializeField] private float detectHeight = 1f;
        [SerializeField] private float detectRadius = 0.5f;
        [SerializeField] private float detectDistance = 1.25f;

        [Header("Visuals")]
        [SerializeField, Range(0f, 1f)] private float handAlpha = 0.55f;
        [SerializeField] private float heightOffset = 0.65f;
        [SerializeField] private float iconScale = 0.43f;
        [SerializeField] private float floatAmplitude = 0.12f;
        [SerializeField] private float floatSpeed = 3.2f;

        private PlayerInteractor _interactor;
        private PlayerEnvironmentDetector _environmentDetector;
        private Camera _mainCamera;
        private GameObject _hintRoot;
        private SpriteRenderer _handRenderer;

        private void Awake()
        {
            _interactor = GetComponent<PlayerInteractor>();
            _environmentDetector = GetComponent<PlayerEnvironmentDetector>();
            _mainCamera = Camera.main;

            if (availableHandSprite == null)
            {
                availableHandSprite = Resources.Load<Sprite>("UI/hand_1");
            }

            if (grabbedHandSprite == null)
            {
                grabbedHandSprite = Resources.Load<Sprite>("UI/hand_2");
            }

            CreateHintObjects();
        }

        private void LateUpdate()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            PushPullInteractable target = GetHintTarget(out bool isGrabbed);
            if (target == null)
            {
                SetHintVisible(false);
                return;
            }

            SetHintVisible(true);
            UpdateHintVisual(target, isGrabbed);
        }

        private PushPullInteractable GetHintTarget(out bool isGrabbed)
        {
            isGrabbed = false;

            if (_interactor != null && _interactor.GrabbedObject != null)
            {
                isGrabbed = true;
                return _interactor.GrabbedObject;
            }

            if (_environmentDetector != null && !_environmentDetector.IsGrounded)
            {
                return null;
            }

            Vector3 rayOrigin = transform.position + Vector3.up * detectHeight;
            if (!Physics.SphereCast(rayOrigin, detectRadius, transform.forward, out RaycastHit hit, detectDistance))
            {
                return null;
            }

            PushPullInteractable interactable = hit.collider.GetComponent<PushPullInteractable>();
            if (interactable == null)
            {
                interactable = hit.collider.GetComponentInParent<PushPullInteractable>();
            }

            return interactable != null && interactable.enabled ? interactable : null;
        }

        private void CreateHintObjects()
        {
            _hintRoot = new GameObject("PushPullHandHint");
            _hintRoot.SetActive(false);

            GameObject handObject = new GameObject("HandIcon");
            handObject.transform.SetParent(_hintRoot.transform, false);
            _handRenderer = handObject.AddComponent<SpriteRenderer>();
            _handRenderer.sortingOrder = 100;
        }

        private void UpdateHintVisual(PushPullInteractable target, bool isGrabbed)
        {
            Sprite targetSprite = isGrabbed ? grabbedHandSprite : availableHandSprite;
            if (_handRenderer != null)
            {
                _handRenderer.sprite = targetSprite;
                _handRenderer.color = new Color(1f, 1f, 1f, handAlpha);
            }

            Collider targetCollider = target.objectCollider != null ? target.objectCollider : target.GetComponentInChildren<Collider>();
            Vector3 basePosition = target.transform.position;
            if (targetCollider != null)
            {
                Bounds bounds = targetCollider.bounds;
                basePosition = bounds.center + Vector3.up * bounds.extents.y;
            }

            float floatOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            _hintRoot.transform.position = basePosition + Vector3.up * (heightOffset + floatOffset);
            _hintRoot.transform.localScale = Vector3.one * iconScale;

            if (_mainCamera != null)
            {
                Vector3 cameraDirection = _hintRoot.transform.position - _mainCamera.transform.position;
                if (cameraDirection.sqrMagnitude > 0.001f)
                {
                    _hintRoot.transform.rotation = Quaternion.LookRotation(cameraDirection);
                }
            }
        }

        private void SetHintVisible(bool visible)
        {
            if (_hintRoot != null && _hintRoot.activeSelf != visible)
            {
                _hintRoot.SetActive(visible);
            }
        }

        private void OnDestroy()
        {
            if (_hintRoot != null)
            {
                Destroy(_hintRoot);
            }
        }
    }
}
