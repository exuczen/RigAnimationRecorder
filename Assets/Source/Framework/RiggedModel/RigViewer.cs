using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DC
{
	public class BonesConnection
	{
		private Transform container;
		private Transform parent;
		private Transform child;
		private GameObject connection;
		private Vector3 localScale;

		public Vector3 LocalScale { get { return localScale; } set { localScale = value; } }

		public BonesConnection(GameObject connectionPrefab, Transform container, Transform parent, Transform child)
		{
			localScale = Vector3.one;
			connection = GameObject.Instantiate(connectionPrefab, container, false);
			connection.name = parent.name + "-" + child.name;
			this.container = container;
			this.parent = parent;
			this.child = child;
			Update();
		}

		public void Update()
		{
			Vector3 middlePos = (parent.position + child.position) / 2;
			Vector3 ray = parent.position - child.position;
			float distance = ray.magnitude;
			Vector3 direction = Vector3.zero;
			connection.transform.position = middlePos;
			connection.transform.localScale = new Vector3(localScale.x, localScale.y, distance / container.lossyScale.z);
			if (distance > 0.00001f)
			{
				direction = ray / distance;
				connection.transform.forward = direction;
			}
		}
	}

	public class RigViewer : MonoBehaviour
	{
		[SerializeField]
		public Transform rootBone;

		[SerializeField]
		public GameObject boneMarker;

		private GameObject boneConnectionPrefab;
		private GameObject boneConnectionsContainer;
		private string hierarchyString;
		private List<BonesConnection> boneConnections = new List<BonesConnection>();
		private List<GameObject> bones = new List<GameObject>();
		private bool isInitialized;

		// Use this for initialization
		private void Start()
		{
			Init();
		}

		private void Init()
		{
			if (!isInitialized && rootBone && boneMarker)
			{
				List<Transform> bonesList = new List<Transform>();
				rootBone.GetComponentsInChildren(bonesList);
				Vector3 boneLocalScale = GetBoneLocalScale(bonesList);
				CreateBoneConnections(boneLocalScale * 0.5f);
				CreateBones(bonesList, boneLocalScale);
				isInitialized = true;
			}
		}

		private void LateUpdate()
		{
			foreach (var boneConnection in boneConnections)
			{
				boneConnection.Update();
			}
		}

		private Vector3 GetBoneLocalScale(List<Transform> bonesList)
		{
			Vector3 localScale;
			if (bonesList.Count > 0)
			{
				localScale = new Vector3(1 / rootBone.lossyScale.x, 1 / rootBone.lossyScale.y, 1 / rootBone.lossyScale.z);
				Vector3 minPos = bonesList[0].position;
				Vector3 maxPos = minPos;
				foreach (var child in bonesList)
				{
					minPos = Maths.Min(minPos, child.position);
					maxPos = Maths.Max(maxPos, child.position);
				}
				Vector3 bounds = maxPos - minPos;
				float maxDim = Mathf.Max(bounds.x, bounds.y, bounds.z);
				localScale *= (maxDim * 0.013f);
			}
			else
			{
				localScale = Vector3.one;
			}
			return localScale;
		}

		private void CreateBoneConnections(Vector3 boneLocalScale)
		{
			boneConnectionPrefab = boneMarker;
			boneConnections.Clear();
			hierarchyString = "RigViewer.CreateBoneConnections\n";
			hierarchyString = string.Concat(hierarchyString, "  " + rootBone.name + "\n");
			boneConnectionsContainer = new GameObject("boneConnections");
			boneConnectionsContainer.transform.SetParent(rootBone.parent, false);
			CreateParentChildBoneConnections(rootBone, 2, boneConnectionsContainer.transform);
			foreach (var boneConnection in boneConnections)
			{
				boneConnection.LocalScale = boneLocalScale;
			}
			Debug.Log(hierarchyString);
		}

		private void CreateBones(List<Transform> bonesList, Vector3 boneLocalScale)
		{
			bones.Clear();
			string bonesString = "RigViewer.CreateBones\n";
			string digitsString = "n4";
			foreach (var child in bonesList)
			{
				GameObject bone = Instantiate(boneMarker, child, false);
				bone.transform.localPosition = Vector3.zero;
				bone.transform.localRotation = Quaternion.identity;
				bone.transform.localScale = boneLocalScale;
				bone.name = child.name + "_marker";
				bones.Add(bone);
				Vector3 localEulerAngles = child.localEulerAngles;
				bonesString = string.Concat(bonesString, child.name, " (", localEulerAngles.x.ToString(digitsString), ",", localEulerAngles.y.ToString(digitsString), ",", localEulerAngles.z.ToString(digitsString), ")\n");
			}
			Debug.Log(bonesString);

			//MeshFilter boneMeshFilter = boneMarker.GetComponent<MeshFilter>();
			//MeshRenderer boneMeshRenderer = boneMarker.GetComponent<MeshRenderer>();
			//MeshFilter meshFiler = child.gameObject.AddComponent<MeshFilter>();
			//meshFiler.sharedMesh = boneMeshFilter.sharedMesh;
			//MeshRenderer meshRenderer = child.gameObject.AddComponent<MeshRenderer>();
			//meshRenderer.sharedMaterial = boneMeshRenderer.sharedMaterial;
		}

		private void OnEnable()
		{
			Init();
			if (isInitialized)
			{
				foreach (var bone in bones)
					bone.SetActive(true);
				if (boneConnectionsContainer)
					boneConnectionsContainer.SetActive(true);
			}
		}

		private void OnDisable()
		{
			if (isInitialized)
			{
				foreach (var bone in bones)
					bone.SetActive(false);
				if (boneConnectionsContainer)
					boneConnectionsContainer.SetActive(false);
			}
		}

		private void CreateParentChildBoneConnections(Transform parent, int depth, Transform container)
		{
			string indentation = "";
			for (int i = 0; i < depth; i++)
				indentation = string.Concat(indentation, "  ");
			foreach (Transform child in parent)
			{
				BonesConnection conn = new BonesConnection(boneConnectionPrefab, container, parent, child);
				boneConnections.Add(conn);
				string line = string.Concat(indentation, child.name);
				hierarchyString = string.Concat(hierarchyString, line, "\n");
				CreateParentChildBoneConnections(child, depth + 1, container);
			}
		}
	}
}