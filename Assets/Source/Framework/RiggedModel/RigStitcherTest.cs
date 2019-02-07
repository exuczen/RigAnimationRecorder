using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DC
{
	public class RigStitcherTest : MonoBehaviour
	{

		public Transform rootBone;
		public SkinnedMeshRenderer dstSkinnedMeshRenderer;
		public GameObject srcSkinnedMeshRendererObjectPrefab;

		private GameObject srcSkinnedMeshRendererObject;

		private void Awake()
		{
			srcSkinnedMeshRendererObject = Instantiate(srcSkinnedMeshRendererObjectPrefab, transform.parent, false);
			SkinnedMeshRenderer srcSkinnedMeshRenderer = srcSkinnedMeshRendererObject.GetComponentInChildren<SkinnedMeshRenderer>();

			dstSkinnedMeshRenderer.PrintBonesInFlatView();
			srcSkinnedMeshRenderer.PrintBonesInFlatView();

			RigStitcher.StitchSkinnedMeshRenderer(srcSkinnedMeshRenderer, dstSkinnedMeshRenderer);
		}
	}
}