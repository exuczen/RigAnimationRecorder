using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DC
{
	public class TransformAnimation
	{
		private AnimationCurve[] localPositionCurves = new AnimationCurve[3];

		private AnimationCurve[] localScaleCurves = new AnimationCurve[3];

		private AnimationCurve[] localRotationCurves = new AnimationCurve[4];

		private Transform transform;

		private string relativePath;

		private bool positionEnabled;

		private bool rotationEnabled;

		private bool scaleEnabled;

		public string RelativePath { get { return relativePath; } }

		public AnimationCurve GetLocalPositionComponentCurve(int componentIndex)
		{
			return localPositionCurves[componentIndex];
		}

		public AnimationCurve GetLocalScaleComponentCurve(int componentIndex)
		{
			return localScaleCurves[componentIndex];
		}

		public AnimationCurve GetLocalRotationComponentCurve(int componentIndex)
		{
			return localRotationCurves[componentIndex];
		}

		public int FramesCount { get { return localRotationCurves[0].length; } }

		public TransformAnimation(string relativePath, Transform transform)
		{
			this.relativePath = relativePath;
			this.transform = transform;
			CreateAnimationCurves();
			SetCurveComponentsEnabled(false, true, false);
		}

		public TransformAnimation(string relativePath) : this(relativePath, null) {}

		public void SetLocalPositionCurveComponentKeyframes(int componentIndex, float[] values, float[] timeline)
		{
			for (int i = 0; i < timeline.Length; i++)
			{
				localPositionCurves[componentIndex].AddKey(timeline[i], values[i]);
			}
		}

		public void SetLocalRotationCurveComponentKeyframes(int componentIndex, float[] values, float[] timeline)
		{
			for (int i = 0; i < timeline.Length; i++)
			{
				localRotationCurves[componentIndex].AddKey(timeline[i], values[i]);
			}
		}

		private void CreateAnimationCurves()
		{
			for (int i = 0; i < 3; i++)
			{
				localPositionCurves[i] = new AnimationCurve();
				localScaleCurves[i] = new AnimationCurve();
			}
			for (int i = 0; i < 4; i++)
			{
				localRotationCurves[i] = new AnimationCurve();
			}
		}

		public void Clear()
		{
			CreateAnimationCurves();
		}

		public void SetCurveComponentsEnabled(bool positions, bool rotations, bool scale)
		{
			positionEnabled = positions;
			rotationEnabled = rotations;
			scaleEnabled = scale;
		}

		public void AddFrame(float time)
		{
			if (positionEnabled)
			{
				for (int i = 0; i < 3; i++)
				{
					localPositionCurves[i].AddKey(time, transform.localPosition[i]);
				}
			}
			if (rotationEnabled)
			{
				for (int i = 0; i < 4; i++)
				{
					Quaternion localRotation = Quaternion.Euler(Maths.AnglesModulo360(transform.localEulerAngles));
					localRotationCurves[i].AddKey(time, localRotation[i]);
				}
			}
			if (scaleEnabled)
			{
				for (int i = 0; i < 3; i++)
				{
					localScaleCurves[i].AddKey(time, transform.localScale[i]);
				}
			}
		}

		public void SetAnimationClipCurves(AnimationClip clip, bool addScaleOnEnds = false)
		{
			Type type = typeof(Transform);

			if (positionEnabled)
			{
				clip.SetCurve(relativePath, type, "localPosition.x", localPositionCurves[0]);
				clip.SetCurve(relativePath, type, "localPosition.y", localPositionCurves[1]);
				clip.SetCurve(relativePath, type, "localPosition.z", localPositionCurves[2]);
			}
			if (rotationEnabled)
			{
				clip.SetCurve(relativePath, type, "localRotation.x", localRotationCurves[0]);
				clip.SetCurve(relativePath, type, "localRotation.y", localRotationCurves[1]);
				clip.SetCurve(relativePath, type, "localRotation.z", localRotationCurves[2]);
				clip.SetCurve(relativePath, type, "localRotation.w", localRotationCurves[3]);
			}
			if (scaleEnabled)
			{
				clip.SetCurve(relativePath, type, "localScale.x", localScaleCurves[0]);
				clip.SetCurve(relativePath, type, "localScale.y", localScaleCurves[1]);
				clip.SetCurve(relativePath, type, "localScale.z", localScaleCurves[2]);
			}
			else if (addScaleOnEnds && rotationEnabled)
			{
				if (localRotationCurves[0].length > 0)
				{
					Keyframe lastLocalRotationXKeyframe = localRotationCurves[0].keys[localRotationCurves[0].length - 1];
					for (int i = 0; i < 3; i++)
					{
						localScaleCurves[i].AddKey(0, 1f);
						localScaleCurves[i].AddKey(lastLocalRotationXKeyframe.time, 1f);
					}
				}
				clip.SetCurve(relativePath, type, "localScale.x", localScaleCurves[0]);
				clip.SetCurve(relativePath, type, "localScale.y", localScaleCurves[1]);
				clip.SetCurve(relativePath, type, "localScale.z", localScaleCurves[2]);
			}
			//Debug.LogWarning(GetType().Name + ". " + relativePath + " " + localPositionCurves[0].keys.Length);
		}
	}
}
