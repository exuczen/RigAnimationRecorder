using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DC
{
	[Serializable]
	public class RigRecordingData
	{
		public List<TransformAnimationData> transformAnimations;
		public List<RotationAnimationData> rotationAnimations;
		public AnimationCurveData timeline;

		public static RigRecordingData FromJson(string json)
		{
			return JsonUtility.FromJson<RigRecordingData>(json);
		}

		public string ToJson()
		{
			return JsonUtility.ToJson(this);
		}

		public RigRecordingData()
		{
			transformAnimations = new List<TransformAnimationData>();
			rotationAnimations = new List<RotationAnimationData>();
		}

		public void AddTimelineData(TransformAnimation transformAnimation)
		{
			AnimationCurve localRotationCurveX = transformAnimation.GetLocalRotationComponentCurve(0);
			timeline = new AnimationCurveData("timeline");
			foreach (var key in localRotationCurveX.keys)
			{
				timeline.AddValue(key.time);
			}
		}

		public void AddTransformAnimationData(TransformAnimation transformAnimation)
		{
			transformAnimations.Add(new TransformAnimationData(transformAnimation));
		}

		public void AddRotationAnimationData(TransformAnimation transformAnimation)
		{
			rotationAnimations.Add(new RotationAnimationData(transformAnimation));
		}

		public AnimationClip CreateAnimationClip(int frameRate, bool legacy)
		{
			AnimationClip clip = new AnimationClip();
			clip.frameRate = frameRate;
			clip.legacy = legacy;
			foreach (var animation in transformAnimations)
			{
				animation.SetAnimationClipCurves(clip, timeline.values);
			}
			foreach (var animation in rotationAnimations)
			{
				animation.SetAnimationClipCurves(clip, timeline.values);
			}
			return clip;
		}
	}

	[Serializable]
	public class RotationAnimationData
	{
		public string relativePath;

		public AnimationCurveData[] localRotationCurveData = new AnimationCurveData[4];

		public RotationAnimationData(TransformAnimation transformAnimation)
		{
			relativePath = transformAnimation.RelativePath;
			string propertyNamePrefix = "localRotation.";
			string[] propertyNameSuffix = new string[4] { "x", "y", "z", "w" };
			for (int i = 0; i < 4; i++)
			{
				localRotationCurveData[i] = new AnimationCurveData(propertyNamePrefix + propertyNameSuffix[i], transformAnimation.GetLocalRotationComponentCurve(i));
			}
		}

		public virtual void SetAnimationClipCurves(AnimationClip clip, List<float> timeline)
		{
			Type type = typeof(Transform);
			string propertyNamePrefix = "localRotation.";
			string[] propertyNameSuffix = new string[4] { "x", "y", "z", "w" };

			if (localRotationCurveData[0].values.Count > 0)
			{
				for (int i = 0; i < 4; i++)
				{
					SetAnimationClipCurve(clip, localRotationCurveData[i], type, propertyNamePrefix + propertyNameSuffix[i], timeline);
				}
			}
		}

		protected void SetAnimationClipCurve(AnimationClip clip, AnimationCurveData curveData, Type type, string propertyName, List<float> timeline)
		{
			if (curveData.values.Count > 0)
			{
				AnimationCurve curve = curveData.CreateAnimationCurve(timeline);
				clip.SetCurve(relativePath, type, propertyName, curve);
			}
		}
	}

	[Serializable]
	public class TransformAnimationData : RotationAnimationData
	{
		public AnimationCurveData[] localPositionCurveData = new AnimationCurveData[3];
		public AnimationCurveData[] localScaleCurveData =  new AnimationCurveData[3];

		public TransformAnimationData(TransformAnimation transformAnimation) : base(transformAnimation)
		{
			string positionPropertyNamePrefix = "localPosition.";
			string scalePropertyNamePrefix = "localScale.";
			string[] propertyNameSuffix = new string[3] { "x", "y", "z" };

			for (int i = 0; i < 3; i++)
			{
				localPositionCurveData[i] = new AnimationCurveData(positionPropertyNamePrefix + propertyNameSuffix[i], transformAnimation.GetLocalPositionComponentCurve(i));
				localScaleCurveData[i] = new AnimationCurveData(scalePropertyNamePrefix + propertyNameSuffix[i], transformAnimation.GetLocalScaleComponentCurve(i));
			}
		}

		public override void SetAnimationClipCurves(AnimationClip clip, List<float> timeline)
		{
			base.SetAnimationClipCurves(clip, timeline);

			Type type = typeof(Transform);
			string positionPropertyNamePrefix = "localPosition.";
			string scalePropertyNamePrefix = "localScale.";
			string[] propertyNameSuffix = new string[3] { "x", "y", "z" };

			if (localPositionCurveData[0].values.Count > 0)
			{
				for (int i = 0; i < 3; i++)
				{
					SetAnimationClipCurve(clip, localPositionCurveData[i], type, positionPropertyNamePrefix + propertyNameSuffix[i], timeline);
				}
			}
			if (localScaleCurveData[0].values.Count > 0)
			{
				for (int i = 0; i < 3; i++)
				{
					SetAnimationClipCurve(clip, localScaleCurveData[i], type, scalePropertyNamePrefix + propertyNameSuffix[i], timeline);
				}
			}
		}
	}

	[Serializable]
	public class AnimationCurveData
	{
		public string propertyName;
		public List<float> values;

		public AnimationCurveData(string propertyName)
		{
			this.propertyName = propertyName;
			values = new List<float>();
		}

		public AnimationCurveData(string propertyName, AnimationCurve animationCurve) : this(propertyName)
		{
			foreach (var key in animationCurve.keys)
			{
				values.Add(key.value);
			}
		}

		public void AddValue(float value)
		{
			values.Add(value);
		}

		public AnimationCurve CreateAnimationCurve(List<float> timeline)
		{
			AnimationCurve animationCurve = new AnimationCurve();
			for (int i = 0; i < values.Count; i++)
			{
				animationCurve.AddKey(timeline[i], values[i]);
			}
			return animationCurve;
		}
	}
}
