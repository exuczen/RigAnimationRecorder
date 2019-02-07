using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterPanels : MonoBehaviour
{
    [SerializeField]
    private RigAnimationRecordingPanel rigAnimationRecordingPanel;

    public RigAnimationRecordingPanel RigAnimationRecordingPanel { get { return rigAnimationRecordingPanel; } }
}
