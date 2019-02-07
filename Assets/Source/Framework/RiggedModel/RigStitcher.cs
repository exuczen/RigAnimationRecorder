using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DC
{
	public class RigStitcher
	{
		public static void StitchSkinnedMeshRendererDeprecated(SkinnedMeshRenderer srcRenderer, SkinnedMeshRenderer dstRenderer, Transform dstRoot)
		{
			Transform[] srcBones = srcRenderer.bones;
			Transform[] bones = new Transform[srcBones.Length];
			int bonesCounter = 0;

			for (int i = 0; i < srcRenderer.bones.Length; i++)
			{
				Transform foundedBone = dstRoot.FindChildInHierarchy(srcBones[i].name);
				if (foundedBone != null)
				{
					bones[bonesCounter++] = foundedBone;
				}
			}

			srcRenderer.bones = bones;
			srcRenderer.rootBone = dstRenderer.rootBone;

			//dstRenderer.sharedMesh = srcRenderer.sharedMesh;
			//dstRenderer.materials = srcRenderer.materials;
			//dstRenderer.sharedMaterial = srcRenderer.sharedMaterial;

			//srcRenderer.PrintBonesInFlatView(bonesCounter);
		}

		public static void StitchSkinnedMeshRenderer(SkinnedMeshRenderer srcRenderer, SkinnedMeshRenderer dstRenderer)
		{
			Transform[] srcBones = srcRenderer.bones;
			Transform[] bones = new Transform[srcBones.Length];
			int bonesCounter = 0;

			List<Transform> dstBonesList = new List<Transform>(dstRenderer.bones);

			for (int i = 0; i < srcRenderer.bones.Length; i++)
			{
				foreach (var dstBone in dstBonesList)
				{
					if (srcBones[i].name.Equals(dstBone.name))
					{
						bones[bonesCounter++] = dstBone;
						dstBonesList.Remove(dstBone);
						break;
					}
				}
			}
			
			srcRenderer.bones = bones;
			srcRenderer.rootBone = dstRenderer.rootBone;

			//dstRenderer.sharedMesh = srcRenderer.sharedMesh;
			//dstRenderer.materials = srcRenderer.materials;
			//dstRenderer.sharedMaterial = srcRenderer.sharedMaterial;

			//srcRenderer.PrintBonesInFlatView(bonesCounter);
		}
	}
}