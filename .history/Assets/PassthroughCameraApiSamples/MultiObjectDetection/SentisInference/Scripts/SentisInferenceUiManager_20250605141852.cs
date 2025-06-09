// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Meta.XR.Samples;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Linq;
using System;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    [MetaCodeSample("PassthroughCameraApiSamples-MultiObjectDetection")]
    public class SentisInferenceUiManager : MonoBehaviour
    {
        [Header("Placement configureation")]
        [SerializeField] private EnvironmentRayCastSampleManager m_environmentRaycast;
        [SerializeField] private WebCamTextureManager m_webCamTextureManager;
        private PassthroughCameraEye CameraEye => m_webCamTextureManager.Eye;

        [Header("UI display references")]
        [SerializeField] private Canvas UICanvas;
        [SerializeField] private SentisObjectDetectedUiManager m_detectionCanvas;
        [SerializeField] private RawImage m_displayImage;
        [SerializeField] private Sprite m_boxTexture;
        [SerializeField] private Color m_boxColor;
        [SerializeField] private Font m_font;
        [SerializeField] private Color m_fontColor;
        [SerializeField] private int m_fontSize = 80;
        [Space(10)]
        public UnityEvent<int> OnObjectsDetected;

        public List<BoundingBox> BoxDrawn = new();
        public List<string> classNames = new List<string>();
        
        // New Added
        public GameObject BoundingBoxPrefab;
        public Transform BoxParent;
        // public GameObject BoundingBox3DPrefab;

        private string[] m_labels;
        private List<GameObject> m_boxPool = new();
        private Transform m_displayLocation;

        //bounding box data
        public struct BoundingBox
        {
            public float CenterX;
            public float CenterY;
            public float Width;
            public float Height;
            public string Label;
            public Vector3? WorldPos;
            public string ClassName;
            public int ClassID;
        }

        #region Unity Functions
        private void Start()
        {
            TextAsset classFile = Resources.Load<TextAsset>("SentisYoloClasses");
            if (classFile != null)
            {
                classNames = classFile.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                Debug.Log($"[Client] Loaded {classNames.Count} YOLO class names.");
            }
            else
            {
                Debug.LogError("[Client] Failed to load SentisYoloClasses.txt from Resources.");
            }
            m_displayLocation = m_displayImage.transform;
        }
        #endregion

        #region Detection Functions
        public void OnObjectDetectionError()
        {
            // Clear current boxes
            ClearAnnotations();

            // Set obejct found to 0
            OnObjectsDetected?.Invoke(0);
        }
        #endregion

        #region BoundingBoxes functions
        
        // ===========================================
        // New Added
        // public void ShowDetections(SentisInferenceRunManager.Detection[] detections)
        // {
        //     if (m_displayImage == null || m_environmentRaycast == null)
        //     {
        //         Debug.LogError("[Client] m_displayImage or m_environmentRaycast is not assigned.");
        //         return;
        //     }

        //     m_detectionCanvas.UpdatePosition();
            
        //     ClearAnnotations(); // Clear existing boxes

        //     var displayWidth = m_displayImage.rectTransform.rect.width;
        //     var displayHeight = m_displayImage.rectTransform.rect.height;
        //     var halfWidth = displayWidth / 2;
        //     var halfHeight = displayHeight / 2;

        //     var boxesFound = detections.Length;
        //     if (boxesFound <= 0)
        //     {
        //         OnObjectsDetected?.Invoke(0);
        //         return;
        //     }
        //     var maxBoxes = Mathf.Min(boxesFound, 200);

        //     OnObjectsDetected?.Invoke(maxBoxes);

        //     var camRes = PassthroughCameraUtils.GetCameraIntrinsics(CameraEye).Resolution;

        //     Debug.Log($"[Client] Drawing {detections.Length} detections");

        //     for (int i = 0; i < boxesFound; i++)
        //     {
        //         var det = detections[i];

        //         float centerX = det.x + det.w / 2f;
        //         float centerY = det.y + det.h / 2f;

        //         // float scaledX = (centerX / Screen.width) * displayWidth - halfWidth;
        //         // float scaledY = (centerY / Screen.height) * displayHeight - halfHeight;
        //         float scaledX = (centerX / Screen.width) * displayWidth - 200;
        //         float scaledY = (centerY / Screen.height) * displayHeight - 300;

        //         float perX = (scaledX + halfWidth) / displayWidth;
        //         float perY = (scaledY + halfHeight) / displayHeight;

        //         // var classname = m_labels[i].Replace(" ", "_");
        //         var classname = (det.@class >= 0 && det.@class < classNames.Count)
        //             ? classNames[det.@class]
        //             : $"Class_{det.@class}";

        //         // Compute 3D ray for interaction
        //         Vector2Int centerPixel = new Vector2Int(
        //             Mathf.RoundToInt(perX * camRes.x),
        //             Mathf.RoundToInt((1.0f - perY) * camRes.y)
        //         );
        //         var ray = PassthroughCameraUtils.ScreenPointToRayInWorld(CameraEye, centerPixel);
        //         var worldPos = m_environmentRaycast.PlaceGameObjectByScreenPos(ray);

        //         // if (m_environmentRaycast.Raycast(ray, out EnvironmentRaycastHit hitInfo))
        //         // {
        //         //     // Place a GameObject at the hit point (position) and rotation (normal)
        //         //     anchorGo.transform.SetPositionAndRotation(
        //         //         hitInfo.point,
        //         //         Quaternion.LookRotation(hitInfo.normal, Vector3.up));
        //         // }

        //         // Add a UI box
        //         var box = new BoundingBox
        //         {
        //             CenterX = scaledX,
        //             CenterY = scaledY,
        //             ClassName = classname,
        //             Width = det.w * displayWidth / Screen.width,
        //             Height = det.h * displayHeight / Screen.height,
        //             Label = $"Id: {i} ClassID: {det.@class} Class: {classname} ({det.confidence:P0})",
        //             // Label = $"Id: {i} ClassID: {det.@class} Class: {classname} Center(px): {det.x},{det.y}",
        //             // Label = $"Id: {i} Class: {det.@class} Center(px): {(int)scaledX},{(int)scaledY} Center(%): {perX:0.00},{perY:0.00}";
        //             WorldPos = worldPos
        //         };

        //         BoxDrawn.Add(box);
        //         DrawBox(box, i);
        //     }

        //     OnObjectsDetected?.Invoke(detections.Length);
        // }
        public void ShowDetections(SentisInferenceRunManager.Detection[] detections)
        {
            if (m_displayImage == null || m_environmentRaycast == null)
            {
                Debug.LogError("[Client] m_displayImage or m_environmentRaycast is not assigned.");
                return;
            }

            m_detectionCanvas.UpdatePosition();
            
            ClearAnnotations(); // Clear existing boxes

            float displayWidth = m_displayImage.rectTransform.rect.width;
            float displayHeight = m_displayImage.rectTransform.rect.height;
            float scaledX = displayWidth / 1280;
            float scaledY = displayHeight / 960;
            float halfWidth = displayWidth / 2;
            float halfHeight = displayHeight / 2;

            var boxesFound = detections.Length;
            if (boxesFound <= 0)
            {
                OnObjectsDetected?.Invoke(0);
                return;
            }
            var maxBoxes = Mathf.Min(boxesFound, 200);

            OnObjectsDetected?.Invoke(maxBoxes);

            var camRes = PassthroughCameraUtils.GetCameraIntrinsics(CameraEye).Resolution;

            Debug.Log($"[Client] Drawing {detections.Length} detections");

            for (int i = 0; i < boxesFound; i++)
            {
                var det = detections[i];

                // float centerX = det.x * scaledX - halfWidth;
                // float centerY = det.y * scaledY - halfHeight;
                float centerX = (det.x + det.w / 2f) * scaledX - halfWidth;
                float centerY = (det.y + det.h / 2f) * scaledY - halfHeight;


                float perX = (centerX + halfWidth) / displayWidth;
                float perY = (centerY + halfHeight) / displayHeight;

                string classname = (det.@class >= 0 && det.@class < classNames.Count)
                    ? classNames[det.@class]
                    : $"Class_{det.@class}";
                // string classname = det.class_name;

                // Compute 3D ray for interaction
                Vector2Int centerPixel = new Vector2Int(
                    Mathf.RoundToInt(perX * camRes.x),
                    Mathf.RoundToInt((1.0f - perY) * camRes.y)
                );
                // Vector2Int centerPixel = new Vector2Int((int)centerX, (int)centerY);
                var ray = PassthroughCameraUtils.ScreenPointToRayInWorld(CameraEye, centerPixel);
                // var worldPos = m_environmentRaycast.PlaceGameObjectByScreenPos(ray);
                Vector3? worldPos = m_environmentRaycast.PlaceGameObjectByScreenPos(ray);
                
                Debug.Log($"Class: {classname}, X: {det.x}, Y: {det.y}, W: {det.w}, H: {det.h}");

                // Add a UI box
                var box = new BoundingBox
                {
                    CenterX = centerX,
                    CenterY = centerY,
                    ClassName = classname,
                    Width = det.w * scaledX,
                    Height = det.h * scaledY,
                    Label = $"Id: {i} ClassID: {det.@class} Class: {classname} ({det.confidence:P0})",
                    // Label = $"Id: {i} ClassID: {det.@class} Class: {classname} Center(px): {det.x},{det.y}",
                    // Label = $"Id: {i} Class: {det.@class} Center(px): {(int)scaledX},{(int)scaledY} Center(%): {perX:0.00},{perY:0.00}";
                    WorldPos = worldPos
                };

                BoxDrawn.Add(box);
                DrawBox(box, i);
            }

            // OnObjectsDetected?.Invoke(detections.Length);
        }
        // public void ShowDetections(SentisInferenceRunManager.Detection[] detections)
        // {
        //     if (m_displayImage == null || m_environmentRaycast == null)
        //     {
        //         Debug.LogError("[Client] m_displayImage or m_environmentRaycast is not assigned.");
        //         return;
        //     }

        //     m_detectionCanvas.UpdatePosition();
        //     ClearAnnotations(); // Clear existing boxes

        //     float displayWidth = m_displayImage.rectTransform.rect.width;
        //     float displayHeight = m_displayImage.rectTransform.rect.height;

        //     var boxesFound = detections.Length;
        //     if (boxesFound <= 0)
        //     {
        //         OnObjectsDetected?.Invoke(0);
        //         return;
        //     }

        //     int maxBoxes = Mathf.Min(boxesFound, 200);
        //     OnObjectsDetected?.Invoke(maxBoxes);

        //     Debug.Log($"[Client] Drawing {maxBoxes} detections");

        //     for (int i = 0; i < maxBoxes; i++)
        //     {
        //         var det = detections[i];

        //         // Optional confidence filter (skip low-confidence detections)
        //         if (det.confidence < 0.4f) continue;

        //         float boxCenterX = det.x + det.w / 2f;
        //         float boxCenterY = det.y + det.h / 2f;

        //         // Normalize to display size (YOLO default size is 640x640)
        //         float uiX = (boxCenterX / 640f) * displayWidth;
        //         float uiY = displayHeight - ((boxCenterY / 640f) * displayHeight);  // Flip Y axis

        //         float boxWidth = (det.w / 640f) * displayWidth;
        //         float boxHeight = (det.h / 640f) * displayHeight;

        //         string classname = (det.@class >= 0 && det.@class < classNames.Count)
        //             ? classNames[det.@class]
        //             : $"Class_{det.@class}";

        //         var box = new BoundingBox
        //         {
        //             CenterX = uiX,
        //             CenterY = uiY,
        //             ClassName = classname,
        //             Width = boxWidth,
        //             Height = boxHeight,
        //             Label = $"Class: {classname} ({det.confidence:P0})",
        //             WorldPos = null  // Not using 3D placement here
        //         };

        //         BoxDrawn.Add(box);
        //         DrawBox(box, i);
        //     }
        // }

        public void SetLabels(TextAsset labelsAsset)
        {
            //Parse neural net m_labels
            m_labels = labelsAsset.text.Split('\n');
        }

        public void SetDetectionCapture(Texture image)
        {
            m_displayImage.texture = image;
            m_detectionCanvas.CapturePosition();
        }

        private void ClearAnnotations()
        {
            foreach (var box in m_boxPool)
            {
                box?.SetActive(false);
            }
            BoxDrawn.Clear();
        }

        private void DrawBox(BoundingBox box, int id)
        {
            GameObject panel;
            if (id < m_boxPool.Count)
            {
                panel = m_boxPool[id];
                if (panel == null) panel = CreateNewBox(m_boxColor);
                else panel.SetActive(true);
            }
            else
            {
                panel = CreateNewBox(m_boxColor);
            }

            var rt = panel.GetComponent<RectTransform>();

            Vector3 finalPosition;
            // Quaternion finalRotation;

            if (box.WorldPos.HasValue)
            {
                finalPosition = box.WorldPos.Value;
                // finalPosition = new Vector3(box.CenterX, -box.CenterY, box.WorldPos.HasValue ? box.WorldPos.Value.z : 0.0f);
                // finalRotation = Quaternion.LookRotation(finalPosition - m_detectionCanvas.GetCapturedCameraPosition(), Vector3.up);
            }
            else
            {
                float fallbackDistance = 2.0f;
                Vector3 screenPoint = new Vector3(box.CenterX + Screen.width / 2f, Screen.height - (box.CenterY + Screen.height / 2f), fallbackDistance);
                finalPosition = Camera.main.ScreenToWorldPoint(screenPoint);
                // finalRotation = Quaternion.LookRotation(finalPosition - m_detectionCanvas.GetCapturedCameraPosition(), Vector3.up);
            }

            panel.transform.position = finalPosition;
            // panel.transform.rotation = finalRotation;
            panel.transform.LookAt(Camera.main.transform);
            panel.transform.Rotate(0, 180f, 0);

            // rt.sizeDelta = new Vector2(box.Width, box.Height);
            // panel.transform.localScale = Vector3.one;

            // float sizeScaleFactor = 0.001f; // tune this if needed
            rt.sizeDelta = new Vector2(box.Width, box.Height);         // set size
            // rt.localScale = new Vector3(sizeScaleFactor, sizeScaleFactor, sizeScaleFactor);

            var label = panel.GetComponentInChildren<Text>();
            label.text = box.Label;
            label.fontSize = 12;
        }
        // private void DrawBox(BoundingBox box, int id)
        // {
        //     GameObject panel;

        //     if (id < m_boxPool.Count)
        //     {
        //         panel = m_boxPool[id];
        //         if (panel == null) panel = CreateNewBox(m_boxColor);
        //         else panel.SetActive(true);
        //     }
        //     else
        //     {
        //         panel = CreateNewBox(m_boxColor);
        //     }

        //     // Ensure the panel is under the display image canvas
        //     panel.transform.SetParent(m_displayImage.transform, false);

        //     var rt = panel.GetComponent<RectTransform>();

        //     // Set position and size
        //     rt.anchoredPosition = new Vector2(box.CenterX, box.CenterY);
        //     rt.sizeDelta = new Vector2(box.Width, box.Height);

        //     // Update label
        //     var label = panel.GetComponentInChildren<Text>();
        //     label.text = box.Label;
        //     label.fontSize = m_fontSize;  // You can adjust this globally
        // }


        private GameObject CreateNewBox(Color color)
        {
            //Create the box and set image
            var panel = new GameObject("ObjectBox");
            _ = panel.AddComponent<CanvasRenderer>();

            var rt = panel.AddComponent<RectTransform>();
            rt.pivot = new Vector2(0.5f, 0.5f);

            var img = panel.AddComponent<Image>();
            img.color = color;
            img.sprite = m_boxTexture;
            img.type = Image.Type.Sliced;
            img.fillCenter = false;
            panel.transform.SetParent(m_displayLocation, false);

            //Create the label
            var text = new GameObject("ObjectLabel");
            _ = text.AddComponent<CanvasRenderer>();
            text.transform.SetParent(panel.transform, false);
            var txt = text.AddComponent<Text>();
            txt.font = m_font;
            txt.color = m_fontColor;
            txt.fontSize = m_fontSize;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;

            var rt2 = text.GetComponent<RectTransform>();
            // rt2.pivot = new Vector2(0.5f, 0.5f); // optional
            // rt2.anchorMin = new Vector2(0, 0);
            // rt2.anchorMax = new Vector2(1, 1);
            // rt2.offsetMin = new Vector2(20, 0);
            // rt2.offsetMax = new Vector2(0, 30);
            rt2.offsetMin = new Vector2(20, rt2.offsetMin.y);
            rt2.offsetMax = new Vector2(0, rt2.offsetMax.y);
            rt2.offsetMin = new Vector2(rt2.offsetMin.x, 0);
            rt2.offsetMax = new Vector2(rt2.offsetMax.x, 30);
            rt2.anchorMin = new Vector2(0, 0);
            rt2.anchorMax = new Vector2(1, 1);

            m_boxPool.Add(panel);
            return panel;
        }
        #endregion
    }
}
