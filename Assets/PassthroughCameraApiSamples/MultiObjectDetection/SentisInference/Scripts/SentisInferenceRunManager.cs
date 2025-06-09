// // Copyright (c) Meta Platforms, Inc. and affiliates.

// using System;
// using System.Collections;
// using Meta.XR.Samples;
// using Unity.Sentis;
// using UnityEngine;

// namespace PassthroughCameraSamples.MultiObjectDetection
// {
//     [MetaCodeSample("PassthroughCameraApiSamples-MultiObjectDetection")]
//     public class SentisInferenceRunManager : MonoBehaviour
//     {
//         [Header("Sentis Model config")]
//         [SerializeField] private Vector2Int m_inputSize = new(640, 640);
//         [SerializeField] private BackendType m_backend = BackendType.CPU;
//         [SerializeField] private ModelAsset m_sentisModel;
//         [SerializeField] private int m_layersPerFrame = 25;
//         [SerializeField] private TextAsset m_labelsAsset;
//         public bool IsModelLoaded { get; private set; } = false;

//         [Header("UI display references")]
//         [SerializeField] private SentisInferenceUiManager m_uiInference;

//         [Header("[Editor Only] Convert to Sentis")]
//         public ModelAsset OnnxModel;
//         [SerializeField, Range(0, 1)] private float m_iouThreshold = 0.6f;
//         [SerializeField, Range(0, 1)] private float m_scoreThreshold = 0.23f;
//         [Space(40)]

//         private Worker m_engine;
//         private IEnumerator m_schedule;
//         private bool m_started = false;
//         private Tensor<float> m_input;
//         private Model m_model;
//         private int m_download_state = 0;
//         private Tensor<float> m_output;
//         private Tensor<int> m_labelIDs;
//         private Tensor<float> m_pullOutput;
//         private Tensor<int> m_pullLabelIDs;
//         private bool m_isWaiting = false;

//         #region Unity Functions
//         private IEnumerator Start()
//         {
//             // Wait for the UI to be ready because when Sentis load the model it will block the main thread.
//             yield return new WaitForSeconds(0.05f);

//             m_uiInference.SetLabels(m_labelsAsset);
//             LoadModel();
//         }

//         private void Update()
//         {
//             InferenceUpdate();
//         }

//         private void OnDestroy()
//         {
//             if (m_schedule != null)
//             {
//                 StopCoroutine(m_schedule);
//             }
//             m_input?.Dispose();
//             m_engine?.Dispose();
//         }
//         #endregion

//         #region Public Functions
//         public void RunInference(Texture targetTexture)
//         {
//             // If the inference is not running prepare the input
//             if (!m_started)
//             {
//                 // clean last input
//                 m_input?.Dispose();
//                 // check if we have a texture from the camera
//                 if (!targetTexture)
//                 {
//                     return;
//                 }
//                 // Update Capture data
//                 m_uiInference.SetDetectionCapture(targetTexture);
//                 // Convert the texture to a Tensor and schedule the inference
//                 m_input = TextureConverter.ToTensor(targetTexture, m_inputSize.x, m_inputSize.y, 3);
//                 m_schedule = m_engine.ScheduleIterable(m_input);
//                 m_download_state = 0;
//                 m_started = true;
//             }
//         }

//         public bool IsRunning()
//         {
//             return m_started;
//         }
//         #endregion

//         #region Inference Functions
//         private void LoadModel()
//         {
//             //Load model
//             var model = ModelLoader.Load(m_sentisModel);
//             Debug.Log($"Sentis model loaded correctly with iouThreshold: {m_iouThreshold} and scoreThreshold: {m_scoreThreshold}");
//             //Create engine to run model
//             m_engine = new Worker(model, m_backend);
//             //Run a inference with an empty input to load the model in the memory and not pause the main thread.
//             var input = TextureConverter.ToTensor(new Texture2D(m_inputSize.x, m_inputSize.y), m_inputSize.x, m_inputSize.y, 3);
//             m_engine.Schedule(input);
//             IsModelLoaded = true;
//         }

//         private void InferenceUpdate()
//         {
//             // Run the inference layer by layer to not block the main thread.
//             if (m_started)
//             {
//                 try
//                 {
//                     if (m_download_state == 0)
//                     {
//                         var it = 0;
//                         while (m_schedule.MoveNext())
//                         {
//                             if (++it % m_layersPerFrame == 0)
//                                 return;
//                         }
//                         m_download_state = 1;
//                     }
//                     else
//                     {
//                         // Get the result once all layers are processed
//                         GetInferencesResults();
//                     }
//                 }
//                 catch (Exception e)
//                 {
//                     Debug.LogError($"Sentis error: {e.Message}");
//                 }
//             }
//         }

//         private void PollRequestOuput()
//         {
//             // Get the output 0 (coordinates data) from the model output using Sentis pull request.
//             m_pullOutput = m_engine.PeekOutput(0) as Tensor<float>;
//             if (m_pullOutput.dataOnBackend != null)
//             {
//                 m_pullOutput.ReadbackRequest();
//                 m_isWaiting = true;
//             }
//             else
//             {
//                 Debug.LogError("Sentis: No data output m_output");
//                 m_download_state = 4;
//             }
//         }

//         private void PollRequestLabelIDs()
//         {
//             // Get the output 1 (labels ID data) from the model output using Sentis pull request.
//             m_pullLabelIDs = m_engine.PeekOutput(1) as Tensor<int>;
//             if (m_pullLabelIDs.dataOnBackend != null)
//             {
//                 m_pullLabelIDs.ReadbackRequest();
//                 m_isWaiting = true;
//             }
//             else
//             {
//                 Debug.LogError("Sentis: No data output m_labelIDs");
//                 m_download_state = 4;
//             }
//         }

//         private void GetInferencesResults()
//         {
//             // Get the different outputs in diferent frames to not block the main thread.
//             switch (m_download_state)
//             {
//                 case 1:
//                     if (!m_isWaiting)
//                     {
//                         PollRequestOuput();
//                     }
//                     else
//                     {
//                         if (m_pullOutput.IsReadbackRequestDone())
//                         {
//                             m_output = m_pullOutput.ReadbackAndClone();
//                             m_isWaiting = false;

//                             if (m_output.shape[0] > 0)
//                             {
//                                 Debug.Log("Sentis: m_output ready");
//                                 m_download_state = 2;
//                             }
//                             else
//                             {
//                                 Debug.LogError("Sentis: m_output empty");
//                                 m_download_state = 4;
//                             }
//                         }
//                     }
//                     break;
//                 case 2:
//                     if (!m_isWaiting)
//                     {
//                         PollRequestLabelIDs();
//                     }
//                     else
//                     {
//                         if (m_pullLabelIDs.IsReadbackRequestDone())
//                         {
//                             m_labelIDs = m_pullLabelIDs.ReadbackAndClone();
//                             m_isWaiting = false;

//                             if (m_labelIDs.shape[0] > 0)
//                             {
//                                 Debug.Log("Sentis: m_labelIDs ready");
//                                 m_download_state = 3;
//                             }
//                             else
//                             {
//                                 Debug.LogError("Sentis: m_labelIDs empty");
//                                 m_download_state = 4;
//                             }
//                         }
//                     }
//                     break;
//                 case 3:
//                     m_uiInference.DrawUIBoxes(m_output, m_labelIDs, m_inputSize.x, m_inputSize.y);
//                     m_download_state = 5;
//                     break;
//                 case 4:
//                     m_uiInference.OnObjectDetectionError();
//                     m_download_state = 5;
//                     break;
//                 case 5:
//                     m_download_state++;
//                     m_started = false;
//                     m_output?.Dispose();
//                     m_labelIDs?.Dispose();
//                     break;
//             }
//         }
//         #endregion
//     }
// }


// Modified for server-based YOLO inference

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using PassthroughCameraSamples;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    public class SentisInferenceRunManager : MonoBehaviour
    {
        [Header("Server Inference Config")]
        public string serverUrl = "https://04ec-172-191-52-165.ngrok-free.app/infer";
        public Camera passthroughCamera;

        [Header("Detection UI")]
        [SerializeField] private SentisInferenceUiManager m_uiInference;
        [SerializeField] private TextAsset m_labelsAsset;
        [SerializeField] private SentisInferenceRunManager inferenceManager;

        private bool m_isWaiting = false;
        public bool IsModelLoaded => true; // Always "loaded" for remote inference
        public WebCamTextureManager webCamTextureManager;
        public UnityEngine.UI.RawImage passthroughRawImage;

        [Range(0f, 1f)]
        public float confidenceThreshold = 0.5f;  // Default: show detections with 50% confidence or higher


        void Start()
        {
            // m_uiInference.SetLabels(m_labelsAsset);
            // Debug.Log("[Client] SentisInferenceRunManager: Start()");
            // StartCoroutine(SendFramesToServerRoutine());
        }

        public void RunInference(Texture targetTexture)
        {
            // No longer needed for server-based inference
            // Left blank to maintain compatibility
        }

        public bool IsRunning()
        {
            return true; // Always return true for remote/server inference
        }

        public void StartDetection()
        {
            if (!IsRunning())
            {
                Debug.Log("[Client] Detection already running.");
                return;
            }

            Debug.Log("[Client] Starting detection manually.");
            StartCoroutine(SendFramesToServerRoutine());
        }

        void Update()
        {
            if (OVRInput.GetDown(OVRInput.Button.One))  // A button
            {
                Debug.Log("[Scene] A button pressed — starting detection.");
                StartDetection();
            }
        }

        IEnumerator SendFramesToServerRoutine()
        {
            Debug.Log("[Client] Coroutine started");

            // Wait for passthrough texture to become available
            while (passthroughRawImage == null || passthroughRawImage.texture == null)
            {
                Debug.Log("[Client] Waiting for passthrough texture...");
                yield return new WaitForSeconds(0.2f);
            }

            Debug.Log("[Client] Passthrough texture is ready");

            while (true)
            {
                if (!m_isWaiting)
                {
                    Debug.Log("[Client] About to send frame to server");
                    yield return new WaitForEndOfFrame();
                    yield return StartCoroutine(SendFrameToServer());
                }
                else
                {
                    yield return null;
                }
            }
        }

        IEnumerator SendFrameToServer()
        {
            m_isWaiting = true;
            Debug.Log("[Client] Capturing frame");

            Texture2D frame = CaptureFrame();
            Debug.Log("[Client] Frame captured");
            
            byte[] imageBytes = frame.EncodeToJPG();
            Debug.Log("[Client] Frame encoded: " + imageBytes.Length + " bytes");

            Destroy(frame);

            UnityWebRequest request = new UnityWebRequest(serverUrl, "POST");
            request.uploadHandler = new UploadHandlerRaw(imageBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/octet-stream");

            Debug.Log("[Client] Sending POST to: " + serverUrl);
            yield return request.SendWebRequest();

            try
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    // string json = request.downloadHandler.text;
                    Debug.Log("[Client] Detections received: " + request.downloadHandler.text);
                    ProcessDetections(request.downloadHandler.text);
                }
                else
                {
                    Debug.LogError("[Client] HTTP Error: " + request.error);
                    Debug.LogError("[Client] Status Code: " + request.responseCode);
                }

                m_isWaiting = false;
            }
            catch (Exception ex)
            {
                Debug.LogError("[Client] Exception in SendFrameToServer: " + ex.Message);
            }
            
        }

        Texture2D CaptureFrame()
        {
            if (passthroughRawImage == null || passthroughRawImage.texture == null)
            {
                Debug.LogError("[Client] RawImage or its texture is not assigned.");
                return null;
            }

            Texture sourceTex = passthroughRawImage.texture;

            RenderTexture rt = new RenderTexture(sourceTex.width, sourceTex.height, 0);
            Graphics.Blit(sourceTex, rt);
            RenderTexture.active = rt;

            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            RenderTexture.active = null;
            rt.Release();

            return tex;
        }

        [Serializable]
        public class Detection
        {
            public int x, y, w, h;
            public float confidence;
            public int @class;
        }

        void ProcessDetections(string json)
        {
            Detection[] detections = JsonHelper.FromJson<Detection>(json);

            if (m_uiInference != null)
            {

                // m_uiInference.ShowDetections(detections);
                var filtered = new List<Detection>();
                foreach (var det in detections)
                {
                    if (det.confidence >= confidenceThreshold)
                        filtered.Add(det);
                }
                m_uiInference.ShowDetections(filtered.ToArray());

            }
            else
            {
                Debug.LogError("[Client] m_uiInference is null. Did you forget to assign it?");
                return;
            }
        }
    }

    // Helper class to handle array JSON (Unity's JsonUtility limitation)
    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            string wrappedJson = "{\"Items\":" + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(wrappedJson);
            return wrapper.Items;
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] Items;
        }
    }
}
