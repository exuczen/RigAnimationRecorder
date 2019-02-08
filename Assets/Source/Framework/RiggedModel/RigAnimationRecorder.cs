#if UNITY_EDITOR
#define SAVE_ANIMATION_CLIP_ASSET
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DC
{
	public class RigAnimationClip : DataFile
	{
		public RigAnimationClip(string filepath) : base(filepath) { }

		public RigAnimationClip(byte[] data) : base(data) { }
	}

	public class RigAnimationRecorder : MonoBehaviour
	{
		private const string VERSION_PREFIX = "RigAnimationRecorder.Version";

		private const string VERSION_SEPARATOR = "#";

		private const int FRAME_RATE = 24;

		[SerializeField]
		private Transform rootBone;

		[SerializeField]
		private Transform hipsBone;

		private Dictionary<Transform, TransformAnimation> transformAnimations = new Dictionary<Transform, TransformAnimation>();

		private float recordingStartTime;

		private bool isRecording;

		public bool IsRecording { get { return isRecording; } }

		private TransformAnimation GetTransformAnimation(Transform t)
		{
			return transformAnimations.ContainsKey(t) ? transformAnimations[t] : null;
		}

		public bool SelfUpdate { get; private set; }

		public void Init()
		{
			CreateTransformAnimationsForHierarchy(rootBone);
		}

		private void DisableCurveComponentsInBoneParents(Transform bone, Transform parent)
		{
			foreach (Transform child in parent)
			{
				if (child == bone)
				{
					return;
				}
				else
				{
					TransformAnimation transformAnimation = GetTransformAnimation(child);
					if (transformAnimation != null)
					{
						transformAnimation.SetCurveComponentsEnabled(false, false, false);
					}
					DisableCurveComponentsInBoneParents(hipsBone, child);
				}
			}
		}

		public void StartRecording(bool selfUpdate, bool positions)
		{
			SelfUpdate = selfUpdate;
			foreach (var kvp in transformAnimations)
			{
				kvp.Value.Clear();
				kvp.Value.SetCurveComponentsEnabled(positions, true, false);
			}
			TransformAnimation hipsTransformAnimation = GetTransformAnimation(hipsBone);
			if (hipsTransformAnimation != null)
			{
				hipsTransformAnimation.SetCurveComponentsEnabled(true, true, false);
			}
			//if (hipsBone != rootBone)
			//{
			//	TransformAnimation rootAnimationTransform = GetTransformAnimation(rootBone);
			//	if (rootAnimationTransform != null)
			//	{
			//		rootAnimationTransform.SetCurveComponentsEnabled(false, false, false);
			//	}
			//	DisableCurveComponentsInBoneParents(hipsBone, rootBone);
			//}
			recordingStartTime = Time.time;
			isRecording = true;
		}

		public void StopRecording()
		{
			isRecording = false;
		}

		public AnimationClip SaveRecording(string dataFilePath, string animationClipsFolderPath, string animationClipName, bool legacy)
		{
			RigRecordingData recording = new RigRecordingData();
			AnimationClip clip = CreateEmptyAnimationClip(legacy);

			TransformAnimation hipsTransformAnimation = GetTransformAnimation(hipsBone);
			hipsTransformAnimation.SetAnimationClipCurves(clip);
			recording.AddTimelineData(hipsTransformAnimation);
			recording.AddTransformAnimationData(hipsTransformAnimation);
			//int hipsKeyIndex = transformAnimations.Keys.ToList().FindIndex(transform => transform == hipsBone);
			transformAnimations.Remove(hipsBone);
			for (int i = 0; i < transformAnimations.Count; i++)
			{
				var kvp = transformAnimations.ElementAt(i);
				TransformAnimation transformAnimation = kvp.Value;
				//Debug.LogWarning(GetType().Name + ".transformAnimation: " + transformAnimation.RelativePath);
				transformAnimation.SetAnimationClipCurves(clip);
				if (transformAnimation.FramesCount > 0)
					recording.AddRotationAnimationData(transformAnimation);
			}
			transformAnimations.Add(hipsBone, hipsTransformAnimation);

			//SaveRecordingToJson(recording, filePath);
			SaveRecordingToBinary(recording, dataFilePath);

			//clip = LoadRecordingFromJson(filePath);
			//clip = LoadRecordingFromBinary(filePath);

			#if SAVE_ANIMATION_CLIP_ASSET
			AssetUtil.CreateAsset(clip, animationClipsFolderPath, animationClipName);
			#endif
			return clip;
		}

		private void SaveRecordingToJson(RigRecordingData recording, string filepath)
		{
			string recordingJSON = recording.ToJson();
			byte[] recordingData = Encoding.ASCII.GetBytes(recordingJSON);
			File.WriteAllBytes(filepath, recordingData);
		}

		public AnimationClip LoadRecordingFromJson(string filePath, bool legacy)
		{
			if (File.Exists(filePath))
			{
				byte[] recordingData = File.ReadAllBytes(filePath);
				string recordingJSON = Encoding.ASCII.GetString(recordingData);
				RigRecordingData recording = RigRecordingData.FromJson(recordingJSON);
				return recording.CreateAnimationClip(FRAME_RATE, legacy);
			}
			return null;
		}

		private void SaveRecordingToBinary(RigRecordingData recording, string filepath)
		{
			int tBonesCount = recording.transformAnimations.Count;
			int rBonesCount = recording.rotationAnimations.Count;
			int framesCount = recording.timeline.values.Count;
			
			if (tBonesCount + rBonesCount <= 0 || framesCount <= 0)
				return;

			using (FileStream fs = File.Create(filepath))
			{
				int offset = 0;
				int recorderVersion = 0;

				// Write recorder version
				byte[] versionPrefixBytes = Encoding.ASCII.GetBytes(VERSION_PREFIX);
				byte[] versionSeparatorBytes = Encoding.ASCII.GetBytes(VERSION_SEPARATOR);
				fs.Write(versionPrefixBytes, offset, versionPrefixBytes.Length);
				fs.Write(versionSeparatorBytes, offset, versionSeparatorBytes.Length);
				fs.Write(BitConverter.GetBytes(recorderVersion), offset, 4);
				fs.Write(versionSeparatorBytes, offset, versionSeparatorBytes.Length);

				// Write size and counter values
				int keyframeValueSize = sizeof(float);
				int singleCurveCapacity = keyframeValueSize * framesCount;
				int singleTransformAnimationCapacity = (4 + 3) * singleCurveCapacity;
				int singleRotationAnimationCapacity = 4 * singleCurveCapacity;
				fs.Write(BitConverter.GetBytes(keyframeValueSize), offset, 4);
				fs.Write(BitConverter.GetBytes(recording.transformAnimations.Count), offset, 4);
				fs.Write(BitConverter.GetBytes(recording.rotationAnimations.Count), offset, 4);
				fs.Write(BitConverter.GetBytes(framesCount), offset, 4);

				// Write timeline
				byte[] singleCurveDataBytes = new byte[singleCurveCapacity];
				string timelineName = recording.timeline.propertyName;
				byte[] timelineNameBytes = Encoding.ASCII.GetBytes(timelineName);
				byte[] timelineNameLengthBytes = BitConverter.GetBytes(timelineNameBytes.Length);
				float[] timelineData = recording.timeline.values.ToArray();
				Buffer.BlockCopy(timelineData, 0, singleCurveDataBytes, 0, singleCurveCapacity);
				fs.Write(timelineNameLengthBytes, offset, 4);
				fs.Write(timelineNameBytes, offset, timelineNameBytes.Length);
				fs.Write(singleCurveDataBytes, offset, singleCurveCapacity);

				// Write animations
				foreach (TransformAnimationData tAnimationData in recording.transformAnimations)
				{
					string boneName = tAnimationData.relativePath;
					byte[] boneNameBytes = Encoding.ASCII.GetBytes(boneName);
					byte[] boneNameLengthBytes = BitConverter.GetBytes(boneNameBytes.Length);

					fs.Write(boneNameLengthBytes, offset, 4);
					fs.Write(boneNameBytes, offset, boneNameBytes.Length);

					for (int i = 0; i < 3; i++)
					{
						Buffer.BlockCopy(tAnimationData.localPositionCurveData[i].values.ToArray(), 0, singleCurveDataBytes, 0, singleCurveCapacity);
						fs.Write(singleCurveDataBytes, offset, singleCurveCapacity);
					}
					for (int i = 0; i < 4; i++)
					{
						Buffer.BlockCopy(tAnimationData.localRotationCurveData[i].values.ToArray(), 0, singleCurveDataBytes, 0, singleCurveCapacity);
						fs.Write(singleCurveDataBytes, offset, singleCurveCapacity);
					}
				}
				foreach (RotationAnimationData rAnimationData in recording.rotationAnimations)
				{
					string boneName = rAnimationData.relativePath;
					byte[] boneNameBytes = Encoding.ASCII.GetBytes(boneName);
					byte[] boneNameLengthBytes = BitConverter.GetBytes(boneNameBytes.Length);

					fs.Write(boneNameLengthBytes, offset, 4);
					fs.Write(boneNameBytes, offset, boneNameBytes.Length);

					for (int i = 0; i < 4; i++)
					{
						Buffer.BlockCopy(rAnimationData.localRotationCurveData[i].values.ToArray(), 0, singleCurveDataBytes, 0, singleCurveCapacity);
						fs.Write(singleCurveDataBytes, offset, singleCurveCapacity);
					}
				}
			}
		}

		private static AnimationClip LoadRecordingFromBinary(string filepath, bool legacy)
		{
			if (!File.Exists(filepath))
				return null;

			//return LoadRecording(File.ReadAllBytes(filepath));
			using (FileStream fileStream = File.OpenRead(filepath))
			{
				return LoadRecordingFromStream(fileStream, false, legacy);
			}
		}

		public static AnimationClip LoadRecording(byte[] data, bool legacy)
		{
			return LoadRecording(new RigAnimationClip(data), legacy);
		}

		public static AnimationClip LoadRecording(RigAnimationClip clip, bool legacy)
		{
			using (Stream stream = new MemoryStream(clip.data))
			{
				return LoadRecordingFromStream(stream, false, legacy);
			}
		}

		private static AnimationClip LoadRecordingFromStream(Stream fs, bool dispose, bool legacy)
		{
			if (fs.Length > 0)
			{
				fs.Position = 0;
				byte[] buffer = new byte[1024];
				//RigRecordingData recording = new RigRecordingData();

				int offset = 0;

				// Read recorder version
				fs.Read(buffer, offset, Encoding.ASCII.GetByteCount(VERSION_PREFIX));
				fs.Read(buffer, offset, Encoding.ASCII.GetByteCount(VERSION_SEPARATOR));
				fs.Read(buffer, offset, 4);
				int recorderVersion = BitConverter.ToInt32(buffer, offset);
				fs.Read(buffer, offset, Encoding.ASCII.GetByteCount(VERSION_SEPARATOR));

				// Read size and counter values
				fs.Read(buffer, offset, 4);
				int keyframeValueSize = BitConverter.ToInt32(buffer, offset);
				fs.Read(buffer, offset, 4);
				int transformAnimationsCount = BitConverter.ToInt32(buffer, offset);
				fs.Read(buffer, offset, 4);
				int rotationAnimationsCount = BitConverter.ToInt32(buffer, offset);
				fs.Read(buffer, offset, 4);
				int framesCount = BitConverter.ToInt32(buffer, offset);

				int singleCurveCapacity = keyframeValueSize * framesCount;
				//int singleTransformAnimationCapacity = (4 + 3) * singleCurveCapacity;
				//int singleRotationAnimationCapacity = 4 * singleCurveCapacity;

				// Read timeline
				byte[] singleCurveDataBuffer = new byte[singleCurveCapacity];
				float[] timelineData = new float[framesCount];
				fs.Read(buffer, offset, 4);
				int timelineNameLength = BitConverter.ToInt32(buffer, offset);
				fs.Read(buffer, offset, timelineNameLength);
				string timelineName = Encoding.ASCII.GetString(buffer, offset, timelineNameLength);
				fs.Read(singleCurveDataBuffer, offset, singleCurveCapacity);
				Buffer.BlockCopy(singleCurveDataBuffer, 0, timelineData, 0, singleCurveCapacity);

				//Debug.LogWarning(GetType().Name + ".LoadRecordingFromBinary: " + recorderVersion + " " + keyframeValueSize + " " + transformAnimationsCount + " " + rotationAnimationsCount + " " + framesCount);
				//Debug.LogWarning(GetType().Name + ".LoadRecordingFromBinary: " + timelineName);
				//for (int i = 0; i < framesCount; i++)
				//{
				//	Debug.LogWarning(GetType().Name + ".LoadRecordingFromBinary: " + timelineData[i]);
				//}

				// Read animations
				AnimationClip clip = CreateEmptyAnimationClip(legacy);
				float[] singleCurveData = new float[framesCount];
				for (int i = 0; i < transformAnimationsCount; i++)
				{
					fs.Read(buffer, offset, 4);
					int boneNameLength = BitConverter.ToInt32(buffer, offset);
					fs.Read(buffer, offset, boneNameLength);
					string boneName = Encoding.ASCII.GetString(buffer, offset, boneNameLength);
					//Debug.LogWarning(GetType().Name + "." + boneName);

					TransformAnimation transformAnimation = new TransformAnimation(boneName);
					transformAnimation.SetCurveComponentsEnabled(true, true, false);
					for (int j = 0; j < 3; j++)
					{
						fs.Read(singleCurveDataBuffer, offset, singleCurveCapacity);
						Buffer.BlockCopy(singleCurveDataBuffer, 0, singleCurveData, 0, singleCurveCapacity);
						transformAnimation.SetLocalPositionCurveComponentKeyframes(j, singleCurveData, timelineData);
					}
					for (int j = 0; j < 4; j++)
					{
						fs.Read(singleCurveDataBuffer, offset, singleCurveCapacity);
						Buffer.BlockCopy(singleCurveDataBuffer, 0, singleCurveData, 0, singleCurveCapacity);
						transformAnimation.SetLocalRotationCurveComponentKeyframes(j, singleCurveData, timelineData);
					}
					transformAnimation.SetAnimationClipCurves(clip);
				}
				for (int i = 0; i < rotationAnimationsCount; i++)
				{
					fs.Read(buffer, offset, 4);
					int boneNameLength = BitConverter.ToInt32(buffer, offset);
					fs.Read(buffer, offset, boneNameLength);
					string boneName = Encoding.ASCII.GetString(buffer, offset, boneNameLength);
					//Debug.LogWarning(GetType().Name + "." + boneName + " " + boneNameLength);

					TransformAnimation transformAnimation = new TransformAnimation(boneName);
					transformAnimation.SetCurveComponentsEnabled(false, true, false);
					for (int j = 0; j < 4; j++)
					{
						fs.Read(singleCurveDataBuffer, offset, singleCurveCapacity);
						Buffer.BlockCopy(singleCurveDataBuffer, 0, singleCurveData, 0, singleCurveCapacity);
						transformAnimation.SetLocalRotationCurveComponentKeyframes(j, singleCurveData, timelineData);
					}
					transformAnimation.SetAnimationClipCurves(clip);
				}
				if (dispose)
				{
					fs.Dispose();
				}
				return clip;
			}
			return null;
		}

		private static AnimationClip CreateEmptyAnimationClip(bool legacy)
		{
			AnimationClip clip = new AnimationClip();
			clip.name = "AnimationClip_" + Time.time;
			clip.frameRate = FRAME_RATE;
			clip.legacy = legacy;
			return clip;
		}

		private void CreateTransformAnimationsForHierarchy(Transform root)
		{
			transformAnimations.Clear();
			//string rootRelativePath = string.Concat("///", root.name);
			string rootRelativePath = root.name;
			transformAnimations.Add(root, new TransformAnimation(rootRelativePath, root));
			CreateTransformAnimationsForChildren(root, rootRelativePath, transformAnimations);
		}

		private void CreateTransformAnimationsForChildren(Transform parent, string parentRelativePath, Dictionary<Transform, TransformAnimation> dict)
		{
			foreach (Transform child in parent)
			{
				string childPath = string.Concat(parentRelativePath, "/", child.name);
				dict.Add(child, new TransformAnimation(childPath, child));
				CreateTransformAnimationsForChildren(child, childPath, dict);
			}
		}

		public void AddFrame(float time)
		{
			bool restoreHipsTransform = false;
			Transform hips = null;
			Quaternion hipsLocalRotation = Quaternion.identity;
			Vector3 hipsLocalPosition = Vector3.zero;
			float elapsedTime = Time.time - recordingStartTime;
			foreach (var kvp in transformAnimations)
			{
				kvp.Value.AddFrame(elapsedTime);
			}
			if (restoreHipsTransform)
			{
				hips.localRotation = hipsLocalRotation;
				hips.localPosition = hipsLocalPosition;
			}
		}

		public void OwnLateUpdate()
		{
			if (isRecording)
			{
				//Debug.LogWarning(GetType().Name + ".OwnLateUpdate:isRecording: time=" + Time.time);
				AddFrame(Time.time);
			}
		}

		private void LateUpdate()
		{
			if (SelfUpdate)
			{
				OwnLateUpdate();
			}
		}

		private void OnEnable()
		{
			
		}

		private void OnDisable()
		{
			
		}

	}
}
