using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FirstBeatMarkerWindow : EditorWindow
{
    private ConductorTuning tuning;
    private FirstBeatTimeline timeline;
    private readonly List<float> markers = new List<float>();
    private Vector2 scroll;

    [MenuItem("Pulse/First Beat Marker")]
    private static void Open() { GetWindow<FirstBeatMarkerWindow>("First Beat Marker"); }

    private void OnGUI()
    {
        tuning = (ConductorTuning)EditorGUILayout.ObjectField("Tuning", tuning, typeof(ConductorTuning), false);
        timeline = (FirstBeatTimeline)EditorGUILayout.ObjectField("Playing Timeline", timeline, typeof(FirstBeatTimeline), true);
        if (timeline == null && GUILayout.Button("Find Scene Timeline"))
            timeline = FindFirstObjectByType<FirstBeatTimeline>();
        if (tuning == null && timeline != null) tuning = timeline.Tuning;
        EditorGUILayout.HelpBox("Enter Play Mode, start the game and tap each actual bar-one with the button or Space while this window is focused. Times are relative to the recording. Review by ear before enabling Verified.", MessageType.Info);
        if (timeline != null)
        {
            EditorGUILayout.LabelField("Recording position", timeline.RecordingPositionSeconds.ToString("F3") + " s");
            EditorGUILayout.LabelField("Preview guide", timeline.UsesDemoGuide ? "NOT aligned to recording" : "Authored markers");
        }
        using (new EditorGUI.DisabledScope(timeline == null || tuning == null ||
            !EditorApplication.isPlaying || timeline.IsPaused || timeline.RecordingPositionSeconds < 0d))
        {
            if (GUILayout.Button("Mark First Beat  [Space]", GUILayout.Height(36))) Mark();
        }
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space)
        {
            Mark();
            Event.current.Use();
        }
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Load From Tuning") && tuning != null)
        {
            markers.Clear();
            if (tuning.firstBeatSeconds != null) markers.AddRange(tuning.firstBeatSeconds);
        }
        using (new EditorGUI.DisabledScope(markers.Count == 0))
            if (GUILayout.Button("Undo Last")) markers.RemoveAt(markers.Count - 1);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("Captured first beats: " + markers.Count);
        if (markers.Count >= 2)
        {
            float duration = markers[markers.Count - 1] - markers[0];
            float bpm = (markers.Count - 1) * 60f * Mathf.Max(1, tuning != null ? tuning.beatsPerBar : 4) / duration;
            EditorGUILayout.LabelField("Average quarter-note BPM", bpm.ToString("F1"));
        }
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(220));
        for (int i = 0; i < markers.Count; i++)
            EditorGUILayout.LabelField((i + 1).ToString(), markers[i].ToString("F3") + " s");
        EditorGUILayout.EndScrollView();
        using (new EditorGUI.DisabledScope(tuning == null || markers.Count == 0))
            if (GUILayout.Button("Save Markers To Tuning (Unverified)"))
            {
                Undo.RecordObject(tuning, "Save first beat markers");
                tuning.firstBeatSeconds = markers.ToArray();
                tuning.recordingBeatMarkersVerified = false;
                EditorUtility.SetDirty(tuning);
                AssetDatabase.SaveAssets();
            }
        if (tuning != null)
            EditorGUILayout.LabelField("Verified", tuning.recordingBeatMarkersVerified.ToString());
    }

    private void Mark()
    {
        if (timeline == null || tuning == null || !EditorApplication.isPlaying ||
            timeline.IsPaused || timeline.RecordingPositionSeconds < 0d ||
            timeline.RecordingPositionSeconds >= timeline.RecordingLengthSeconds) return;
        float time = (float)timeline.RecordingPositionSeconds;
        if (markers.Count > 0 && time <= markers[markers.Count - 1]) return;
        markers.Add(time);
        Repaint();
    }

    private void Update() { if (EditorApplication.isPlaying) Repaint(); }
}
