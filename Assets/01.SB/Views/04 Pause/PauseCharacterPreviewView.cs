using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseCharacterPreviewView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Preview")]
    [SerializeField] private GameObject previewWorldRoot;
    [SerializeField] private RawImage previewImage;
    [SerializeField] private RectTransform previewArea;
    [SerializeField] private Camera previewCamera;
    [SerializeField] private Transform modelRoot;
    [SerializeField] private Transform cameraPivot;

    [Header("Rotate")]
    [SerializeField] private float rotateSpeed = 90f;

    [Header("Zoom")]
    [SerializeField] private float minCameraDistance = 1.2f;
    [SerializeField] private float maxCameraDistance = 3f;
    [SerializeField] private float zoomSpeed = 0.4f;

    [Header("Pan")]
    [SerializeField] private Vector2 maxPanOffset = new Vector2(0.5f, 0.35f);
    [SerializeField] private float panSpeed = 0.01f;
    [SerializeField] private float resetLerpSpeed = 8f;

    private bool isPointerInside;
    private bool isDragging;
    private float currentDistance;
    private Vector2 currentPanOffset;
    private Vector2 lastMousePosition;

    private Quaternion initialModelRotation;
    private Vector3 initialCameraLocalPosition;
    private Vector3 initialPivotLocalPosition;
    private float initialDistance;

    private void Awake()
    {
        if (previewArea == null && previewImage != null)
        {
            previewArea = previewImage.rectTransform;
        }

        if (modelRoot != null)
        {
            initialModelRotation = modelRoot.rotation;
        }

        if (previewCamera != null)
        {
            initialCameraLocalPosition = previewCamera.transform.localPosition;
            initialDistance = Mathf.Abs(initialCameraLocalPosition.z);
            currentDistance = Mathf.Clamp(initialDistance, minCameraDistance, maxCameraDistance);
        }

        if (cameraPivot != null)
        {
            initialPivotLocalPosition = cameraPivot.localPosition;
        }
    }

    private void OnEnable()
    {
        SetPreviewWorldVisible(true);
        ResetPreview();
    }

    private void OnDisable()
    {
        isPointerInside = false;
        isDragging = false;

        SetPreviewWorldVisible(false);
    }

    private void Update()
    {
        HandleRotateInput();
        HandleZoomInput();
        HandlePanInput();
        ApplyPreviewTransform();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        isDragging = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!CanPan())
            return;

        isDragging = true;
        lastMousePosition = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        isDragging = false;
    }

    public void ResetPreview()
    {
        isPointerInside = false;
        isDragging = false;
        currentPanOffset = Vector2.zero;
        lastMousePosition = Vector2.zero;
        currentDistance = Mathf.Clamp(initialDistance, minCameraDistance, maxCameraDistance);

        if (modelRoot != null)
        {
            modelRoot.rotation = initialModelRotation;
        }

        if (previewCamera != null)
        {
            previewCamera.transform.localPosition = initialCameraLocalPosition;
        }

        if (cameraPivot != null)
        {
            cameraPivot.localPosition = initialPivotLocalPosition;
        }

        ApplyPreviewTransform();
    }

    private void SetPreviewWorldVisible(bool isVisible)
    {
        if (previewWorldRoot != null) previewWorldRoot.SetActive(isVisible);

        if (previewImage != null) previewImage.enabled = isVisible;

        if (previewCamera != null) previewCamera.enabled = isVisible;
    }

    private void HandleRotateInput()
    {
        if (modelRoot == null)
            return;

        float direction = 0f;

        if (Input.GetKey(KeyCode.LeftBracket))
        {
            direction = 1f;
        }
        else if (Input.GetKey(KeyCode.RightBracket))
        {
            direction = -1f;
        }

        if (Mathf.Approximately(direction, 0f))
            return;

        modelRoot.Rotate(Vector3.up, direction * rotateSpeed * Time.unscaledDeltaTime, Space.World);
    }

    private void HandleZoomInput()
    {
        if (!isPointerInside)
            return;

        float scroll = Input.mouseScrollDelta.y;

        if (Mathf.Approximately(scroll, 0f))
            return;

        currentDistance -= scroll * zoomSpeed;
        currentDistance = Mathf.Clamp(currentDistance, minCameraDistance, maxCameraDistance);
    }

    private void HandlePanInput()
    {
        if (!CanPan())
        {
            currentPanOffset = Vector2.Lerp(
                currentPanOffset,
                Vector2.zero,
                resetLerpSpeed * Time.unscaledDeltaTime);

            return;
        }

        if (!isDragging)
            return;

        Vector2 currentMousePosition = Input.mousePosition;
        Vector2 delta = currentMousePosition - lastMousePosition;
        lastMousePosition = currentMousePosition;

        currentPanOffset += delta * panSpeed;
        currentPanOffset.x = Mathf.Clamp(currentPanOffset.x, -maxPanOffset.x, maxPanOffset.x);
        currentPanOffset.y = Mathf.Clamp(currentPanOffset.y, -maxPanOffset.y, maxPanOffset.y);
    }

    private void ApplyPreviewTransform()
    {
        if (previewCamera != null)
        {
            Vector3 cameraPosition = previewCamera.transform.localPosition;
            cameraPosition.z = -currentDistance;
            previewCamera.transform.localPosition = cameraPosition;
        }

        if (cameraPivot != null)
        {
            Vector3 pivotPosition = cameraPivot.localPosition;
            pivotPosition.x = currentPanOffset.x;
            pivotPosition.y = currentPanOffset.y;
            cameraPivot.localPosition = pivotPosition;
        }
    }

    private bool CanPan()
    {
        if (!isPointerInside)
            return false;

        return currentDistance < maxCameraDistance - 0.05f;
    }
}
