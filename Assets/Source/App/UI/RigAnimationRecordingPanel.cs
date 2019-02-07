using DC;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class RigAnimationRecordingPanel : MonoBehaviour
{
    [SerializeField]
    private RigAnimationRecorder recorder;

    [SerializeField]
    private SkinnedMeshRenderer mesh;

    [SerializeField]
    private Toggle meshToggle;

    [SerializeField]
    private Button recordingButton;

    [SerializeField]
    private Button cancelReplayButton;

    [SerializeField]
    private Text recordingTimeText;

    [SerializeField]
    private Text replayText;

    private Text recordingButtonText;

    private Image recordingButtonImage;

    private float recordingStartTime;

    private float replayStartTime;

    private Animator animator;

    private enum State
    {
        Idle,
        Recording,
        RecordedIdle,
        Replay,
    }

    private enum TriggerName
    {
        recorded,
        idle
    }

    private State state;

    private static string DataFilePath { get { return Application.persistentDataPath + "/RecordedAnimation.dat"; } }

    private const string AnimationClipsFolderPath = "Assets/RecordedAnimations";

    private const string AnimationClipName = "RecorderAnimation.anim";

    private const string RecordingButtonStartRecText = "Start rec.";

    private const string RecordingButtonStopRecText = "Stop rec.";

    private const string RecordingButtonPlayText = "Play";

    private const string RecordingButtonStopText = "Stop";

    private const string RecordedClipKeyName = "Recorded";

    private const string StartTimeText = "00:00";

    private static RigAnimationRecordingPanel instance;

    public static RigAnimationRecordingPanel Instance { get { return instance; } }

    private void Awake()
    {
        recorder.Init();
        recordingButton.onClick.AddListener(OnRecordingButtonClick);
        recordingButtonText = recordingButton.GetComponentInChildren<Text>();
        recordingButtonImage = recordingButton.GetComponent<Image>();
        recordingTimeText.text = StartTimeText;
        animator = recorder.GetComponent<Animator>();
        cancelReplayButton.onClick.AddListener(OnCancelReplayButtonClick);
        cancelReplayButton.gameObject.SetActive(false);
        replayText.gameObject.SetActive(false);
        meshToggle.onValueChanged.AddListener(value => { mesh.enabled = value; });
        state = State.Idle;
        instance = this;
    }

    private void Update()
    {
        if (state == State.Recording || state == State.Replay)
        {
            UpdateRecordingTimeText();
        }
    }

    private void LateUpdate()
    {
        recorder.OwnLateUpdate();
    }

    public void OnRecordedStateExit()
    {
        if (state == State.Replay)
        {
            recordingButtonText.text = RecordingButtonPlayText;
            recordingTimeText.text = StartTimeText;
            replayText.gameObject.SetActive(false);
            state = State.RecordedIdle;
        }
    }

    private void SetTrigger(TriggerName trigger)
    {
        animator.SetTrigger(trigger.ToString());
    }

    private void UpdateRecordingTimeText()
    {
        float elapsedTime = Time.time - recordingStartTime;
        int minutes = (int)(elapsedTime / 60f);
        int seconds = (int)(elapsedTime - minutes * 60);
        string minutesString = minutes.ToString();
        string secondsString = seconds.ToString();
        if (minutes < 10)
            minutesString = "0" + minutesString;
        if (seconds < 10)
            secondsString = "0" + secondsString;
        recordingTimeText.text = minutesString + ":" + secondsString;
    }

    private void OnCancelReplayButtonClick()
    {
        switch (state)
        {
            case State.Idle:
                Debug.LogWarning(GetType() + ".OnCancelReplayButtonClick in " + state);
                break;
            case State.Recording:
                Debug.LogWarning(GetType() + ".OnCancelReplayButtonClick in " + state);
                break;
            case State.RecordedIdle:
            case State.Replay:
                if (state == State.Replay)
                    SetTrigger(TriggerName.idle);
                cancelReplayButton.gameObject.SetActive(false);
                replayText.gameObject.SetActive(false);
                recordingButtonText.text = RecordingButtonStartRecText;
                recordingTimeText.text = StartTimeText;
                state = State.Idle;
                break;
            default:
                break;
        }
    }

    private void OnRecordingButtonClick()
    {
        recordingTimeText.text = StartTimeText;
        switch (state)
        {
            case State.Idle:
                if (!recorder.IsRecording)
                {
                    recordingStartTime = Time.time;
                    recorder.StartRecording(false);
                    recordingButtonText.text = RecordingButtonStopRecText;
                    recordingButtonImage.color = Color.red;
                    state = State.Recording;
                }
                break;
            case State.Recording:
                if (recorder.IsRecording)
                {
                    recorder.StopRecording();
                    AnimationClip clip = recorder.SaveRecording(DataFilePath, AnimationClipsFolderPath, AnimationClipName, false);
                    OverrideAnimationClip(RecordedClipKeyName, clip);
                    Debug.Log(GetType() + ".OnRecordingButtonClick: animation clip saved to " + Path.Combine(AnimationClipsFolderPath, AnimationClipName));
                    cancelReplayButton.gameObject.SetActive(true);
                    recordingButtonText.text = RecordingButtonPlayText;
                    recordingButtonImage.color = Color.white;
                    state = State.RecordedIdle;
                }
                break;
            case State.RecordedIdle:
                // start replay
                recordingStartTime = Time.time;
                recordingButtonText.text = RecordingButtonStopText;
                replayText.gameObject.SetActive(true);
                SetTrigger(TriggerName.recorded);
                state = State.Replay;
                break;
            case State.Replay:
                // stop replay;
                recordingButtonText.text = RecordingButtonPlayText;
                replayText.gameObject.SetActive(false);
                SetTrigger(TriggerName.idle);
                state = State.RecordedIdle;
                break;
            default:
                break;
        }
    }

    public void OverrideAnimationClip(string keyClipName, AnimationClip newClip)
    {
        AnimatorOverrideController aoc = new AnimatorOverrideController(animator.runtimeAnimatorController);
        List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        aoc.GetOverrides(overrides);
        int keyClipIndex = overrides.FindIndex(kvp => kvp.Key.name.Equals(keyClipName));
        //Debug.Log(GetType() + ".OverrideAnimationClip: keyClipIndex=" + keyClipIndex);
        if (keyClipIndex >= 0)
        {
            AnimationClip keyClip = overrides[keyClipIndex].Key;
            overrides[keyClipIndex] = new KeyValuePair<AnimationClip, AnimationClip>(keyClip, newClip);
        }
        //for (int i = 0; i < overrides.Count; i++)
        //{
        //    Debug.Log(GetType() + ".OverrideAnimationClip: key=" + overrides[i].Key + " value=" + overrides[i].Value);
        //}
        aoc.name = aoc.name + "Overrided";
        aoc.ApplyOverrides(overrides);
        animator.runtimeAnimatorController = aoc;
    }

}
