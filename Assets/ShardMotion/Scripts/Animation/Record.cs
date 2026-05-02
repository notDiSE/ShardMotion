using System.Collections;
using ShardMotion;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ShardMotion.Animation
{
    public class Record : MonoBehaviour
    {
    #if UNITY_EDITOR
        [HideInInspector]
        public bool recording;
    
        [HideInInspector]
        public bool playback;

        private Coroutine _routine;

        public int fps = 24;
        private float _time = 0;
        private string _tempAssetPath = "Assets/tmp.anim";

        private AnimationClip _clip;
        private Animator _animator;
    
        PlayableGraph _graph;
        AnimationClipPlayable _playable;

        private void Start()
        {
            _animator = GetComponent<Animator>();
        }

        public void PlayRecording()
        {
            if (_clip == null) return;
        
            playback = true;
        
            if (_graph.IsValid())
                _graph.Destroy();

            _animator.enabled = true;

            _graph = PlayableGraph.Create("RecordedClip");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            var output = AnimationPlayableOutput.Create(_graph, "AnimOutput", _animator);
            _playable = AnimationClipPlayable.Create(_graph, _clip);
            _playable.SetApplyFootIK(false);
            _playable.SetApplyPlayableIK(false);

            output.SetSourcePlayable(_playable);

            _graph.Play();
        }

        public void LoadAndPlayRecording()
        {
            string path = EditorUtility.OpenFilePanel("Select Animation Clip", "Assets", "anim");
            if (string.IsNullOrEmpty(path)) return;
            
            if (path.StartsWith(Application.dataPath))
                path = "Assets" + path.Substring(Application.dataPath.Length);

            AnimationClip loaded = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (loaded == null)
            {
                Debug.LogWarning($"[ShardMotion] Could not load AnimationClip at: {path}");
                return;
            }

            _clip = loaded;
            PlayRecording();
        }

        public void StopPlayingRecording()
        {
            playback = false;
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }

            _animator.enabled = false;
        }

        public void CreateTempClip()
        {
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(_tempAssetPath) != null)
                AssetDatabase.DeleteAsset(_tempAssetPath);
            _clip = WriteAnim.CreateClipAsset(_tempAssetPath, true);
        }

        public void StartRecording()
        {
            if (_routine != null) StopCoroutine(_routine);
            CreateTempClip();
            recording = true;
            _time = 0;
            _routine = StartCoroutine(RecordRoutine());
        }

        private void Update()
        {
            if (recording) _time += Time.deltaTime;
        }

        IEnumerator RecordRoutine()
        {
            float delay = 1f / fps;
            while (recording)
            {
                RecordFrame();
                yield return new WaitForSeconds(delay);
            }
        }

        void RecordFrame()
        {
            foreach (ArUcoTarget target in ArUcoRegistry.All)
            {
                if (!target.tracked) continue;
                WriteAnim.AddKey(_clip, transform, target.transform, target.transform.localPosition, target.transform.localRotation, _time);
            }
            EditorUtility.SetDirty(_clip);
        }

        public void StopRecording()
        {
            recording = false;
            if (_routine != null) StopCoroutine(_routine);
            AssetDatabase.SaveAssets();

            // Ask user where to save the clip
            string savePath = EditorUtility.SaveFilePanelInProject(
                "Save Animation Clip",
                "RecordedAnimation",
                "anim",
                "Choose where to save the recorded animation"
            );

            if (!string.IsNullOrEmpty(savePath))
            {
                string error = AssetDatabase.MoveAsset(_tempAssetPath, savePath);
                if (!string.IsNullOrEmpty(error))
                {
                    AssetDatabase.DeleteAsset(savePath);
                    AssetDatabase.MoveAsset(_tempAssetPath, savePath);
                }
                _clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(savePath);
                Debug.Log($"[ShardMotion] Animation saved to: {savePath}");
            }
            else
            {
                Debug.Log("[ShardMotion] Save cancelled — clip kept at Assets/tmp.anim");
            }
        }
    
        private void OnDisable()
        {
            if (_graph.IsValid())
                _graph.Destroy();
        }
    #endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(Record))]
    public class RecordEditor : Editor
    {
        Record targetScript;

        public void Awake()
        {
            targetScript = (Record)target;
        }

        public override void OnInspectorGUI()
        {
            if (!targetScript.recording)
            {
                if (GUILayout.Button("Record"))
                {
                    targetScript.StartRecording();
                }

                if (!targetScript.playback)
                {
                    if (GUILayout.Button("Load & Playback"))
                        targetScript.LoadAndPlayRecording();
                }
                else
                {
                    if (GUILayout.Button("Stop Playback"))
                        targetScript.StopPlayingRecording();
                }
            }
            else
            {
                if (GUILayout.Button("Stop Recording"))
                {
                    targetScript.StopRecording();
                }
            }
            base.OnInspectorGUI();
        }
    }
#endif
}