using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DC
{
	public static class ReflectionUtil
	{
		/// <summary>
		/// Finds object of given type in owner properties
		/// </summary>
		/// <param name="type">Type of object to find in owner properties</param>
		/// <param name="owner">Owner of object to find</param>
		/// <returns></returns>
		public static object FindObjectOfTypeInOwnerProperties(Type type, object owner)
		{
			List <PropertyInfo> ownerProperties = owner.GetType().GetProperties().ToList();
			return FindObjectOfTypeInOwnerProperties(type, owner, ownerProperties);
		}

		/// <summary>
		/// Finds object of given type in owner properties
		/// </summary>
		/// <param name="type">Type of object to find in owner properties</param>
		/// <param name="owner">Owner of object to find</param>
		/// <param name="ownerProperties">Owner properties list</param>
		/// <returns></returns>
		public static object FindObjectOfTypeInOwnerProperties(Type type, object owner, List<PropertyInfo> ownerProperties)
		{
			PropertyInfo objectProperty = ownerProperties.Find(propertyInfo => propertyInfo.PropertyType == type);
			return objectProperty != null ? objectProperty.GetValue(owner, null) : null;
		}

		/// <summary>
		/// Finds object of given type that derives from or implements interface of generic type T
		/// </summary>
		/// <typeparam name="T">Base class type</typeparam>
		/// <param name="type">Derived class type</param>
		/// <param name="owner">Owner of object property of derived class</param>
		/// <param name="ownerProperties">Owner properties list</param>
		/// <returns></returns>
		public static T FindObjectOfDerivedTypeInOwnerProperties<T>(Type type, object owner, List<PropertyInfo> ownerProperties) where T : UnityEngine.Object
		{
			object obj = FindObjectOfTypeInOwnerProperties(type, owner, ownerProperties);
			return obj != null && typeof(T).IsAssignableFrom(type) ? obj as T : null;
		}
	}

	public static class SceneUtil
	{
		public static bool IsLoadingScene { get; private set; }

		public static string ActiveSceneName { get { return SceneManager.GetActiveScene().name; } }

		public static T GetActiveSceneName<T>()
		{
			return ActiveSceneName.ParseToEnum<T>();
		}

		public static Coroutine LoadSceneAsync(MonoBehaviour context, string sceneName, LoadSceneMode mode, Action<string> preLoad = null, Action<float> onProgress = null, Action<Scene> onComplete = null)
		{
			if (!IsLoadingScene && !ActiveSceneName.Equals(sceneName))
			{
				IsLoadingScene = true;
				if (preLoad != null)
					preLoad(sceneName);
				return context.StartCoroutine(LoadSceneAsyncRoutine(sceneName, mode, onProgress, scene => {
					if (onComplete != null)
						onComplete(scene);
					IsLoadingScene = false;
				}));
			}
			return null;
		}

		private static IEnumerator LoadSceneAsyncRoutine(string sceneName, LoadSceneMode mode, Action<float> onProgress = null, Action<Scene> onComplete = null)
		{
			yield return new WaitForEndOfFrame();

			// The Application loads the Scene in the background as the current Scene runs.
			// This is particularly good for creating loading screens.
			// You could also load the Scene by using sceneBuildIndex. In this case Scene2 has
			// a sceneBuildIndex of 1 as shown in Build Settings.

			AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, mode);

			// Wait until the asynchronous scene fully loads
			while (!asyncLoad.isDone)
			{
				if (onProgress != null)
					onProgress(asyncLoad.progress);
				yield return null;
			}
			if (onProgress != null)
				onProgress(1f);

			if (onComplete != null)
				onComplete(SceneManager.GetSceneByName(sceneName));
		}

		public static Scene GetDontDestroyOnLoadScene()
		{
			GameObject temp = null;
			try
			{
				temp = new GameObject();
				UnityEngine.Object.DontDestroyOnLoad(temp);
				Scene scene = temp.scene;
				UnityEngine.Object.DestroyImmediate(temp);
				temp = null;
				return scene;
			}
			finally
			{
				if (temp != null)
					UnityEngine.Object.DestroyImmediate(temp);
			}
		}

		public static Canvas FindCanvas(Scene scene, Transform persistentCanvasTransform)
		{
			List<GameObject> rootGameObjects = new List<GameObject>();
			scene.GetRootGameObjects(rootGameObjects);
			Canvas sceneCanvas = null;
			rootGameObjects.Find(root => { return root.transform != persistentCanvasTransform && (sceneCanvas = root.GetComponent<Canvas>()) != null; });
			if (!sceneCanvas)
				rootGameObjects.Find(root => { return (sceneCanvas = root.GetComponentInChildren<Canvas>()) != null; });
			return sceneCanvas;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="scene"></param>
		/// <param name="onlyFirstInRoot">depth first search</param>
		/// <returns></returns>
		public static List<T> FindObjectsOfType<T>(Scene scene, bool onlyFirstInRoot = false) where T : Component
		{
			List<T> results = new List<T>();
			List<GameObject> roots = scene.GetRootGameObjects().ToList();
			if (onlyFirstInRoot)
			{
				foreach (GameObject root in roots)
				{
					T component = root.GetComponentInChildren<T>(true);
					if (component)
						results.Add(component);
				}
			}
			else
			{
				foreach (GameObject root in roots)
				{
					results.AddRange(root.GetComponentsInChildren<T>(true));
				}
			}
			return results;
		}
	}

	public static class AssetUtil
	{
		#if UNITY_EDITOR
		public static void CreateAsset(UnityEngine.Object asset, string assetDirPath, string assetFileName)
		{
			if (!Directory.Exists(assetDirPath))
				Directory.CreateDirectory(assetDirPath);
			string assetFilePath = Path.Combine(assetDirPath, assetFileName);
			UnityEditor.AssetDatabase.DeleteAsset(assetFilePath);
			UnityEditor.AssetDatabase.CreateAsset(asset, assetFilePath);
			UnityEditor.AssetDatabase.SaveAssets();
			UnityEditor.AssetDatabase.Refresh();
		}
		#endif
	}

	public static class LoopUtil
	{
		public static IEnumerator DeltaLoopRoutine(int startIndex, int count, int delta, Action<int> preLoop, Action<int> inLoop, Action<int> postLoop)
		{
			for (int i = startIndex; i < count; i += delta)
			{
				//Debug.LogWarning("LoopRoutine: " + i);
				preLoop(i);
				int max = Math.Min(i + delta, count);
				for (int j = i; j < max; j++)
				{
					inLoop(j);
				}
				postLoop(max - 1);
				yield return null;
			}
		}
	}

	public static class WWWUtil
	{
		public static void LoadBinaryFromWWW(MonoBehaviour context, string url, Action<byte[]> onDownloadSuccess, Action<string> onDownloadError = null)
		{
			context.StartCoroutine(LoadBinaryFromWWWRoutine(url, onDownloadSuccess, onDownloadError));
		}

		public static IEnumerator LoadBinaryFromWWWRoutine(string url, Action<byte[]> onDownloadSuccess, Action<string> onDownloadError = null, float timeout = 30f)
		{
			WWW www = new WWW(url);
			float startTime = Time.time;
			float elapsedTime = 0;
			yield return new WaitWhile(() => (elapsedTime = Time.time - startTime) < timeout && !www.isDone);
			if (www.isDone)
			{
				yield return www;
				if (string.IsNullOrEmpty(www.error))
				{
					if (www.bytes != null && www.bytes.Length > 0)
					{
						onDownloadSuccess(www.bytes);
					}
					else if (onDownloadError != null)
					{
						onDownloadError("www.bytes = null || www.bytes.Length = 0");
					}
				}
				else if (onDownloadError != null)
				{
					onDownloadError(www.error);
				}
			}
			else
			{
				www.Dispose();
				if (onDownloadError != null)
					onDownloadError("timeout");
			}
		}
	}

	public static class ScreenUtil
	{
		public static float AspectRatio = (float)Screen.width / (float)Screen.height;
		public static float AspectRatioInv = (float)Screen.height / (float)Screen.width;
	}

	public static class SpriteRendererUtil
	{
		public static void PlaceInCameraView(this SpriteRenderer spriteRenderer, Camera camera, float destWidth, float worldDistance, SpriteAlignment anchor)
		{
			float ratio = 1f;
			Sprite sprite = spriteRenderer.sprite;
			if (sprite != null && sprite.texture != null)
			{
				Texture texture = sprite.texture;
				if (texture != null)
				{
					ratio = (float)texture.width / (float)texture.height;
				}
			}
			spriteRenderer.PlaceInCameraView(camera, destWidth, ratio, worldDistance, anchor);
		}

		public static void PlaceInCameraView(this Renderer renderer, Camera camera, float destWidth, float ratio, float worldDistance, SpriteAlignment anchor)
		{
			ratio = (ratio >= 0.01f ? ratio : 1f);
			//Debug.LogWarning("PlaceInCameraView: ratio = " + ratio);
			float destHalfWidth = destWidth / 2;
			float destHalfHeight = destHalfWidth / ratio;
			Vector3 screenPt1 = Vector3.zero;
			Vector3 screenPt2 = Vector3.zero;
			switch (anchor)
			{
				//case SpriteAlignment.Center:
				//	break;
				case SpriteAlignment.TopLeft:
					screenPt1 = new Vector3(destHalfWidth, Screen.height - destHalfHeight, worldDistance);
					screenPt2 = new Vector3(destHalfWidth * 2, Screen.height, worldDistance);
					break;
				case SpriteAlignment.TopCenter:
					screenPt1 = new Vector3(Screen.width / 2, Screen.height - destHalfHeight, worldDistance);
					screenPt2 = new Vector3(Screen.width / 2 + destHalfWidth, Screen.height, worldDistance);
					break;
				case SpriteAlignment.TopRight:
					screenPt1 = new Vector3(Screen.width - destHalfWidth, Screen.height - destHalfHeight, worldDistance);
					screenPt2 = new Vector3(Screen.width, Screen.height, worldDistance);
					break;
				//case SpriteAlignment.LeftCenter:
				//	break;
				//case SpriteAlignment.RightCenter:
				//	break;
				//case SpriteAlignment.BottomLeft:
				//	break;
				//case SpriteAlignment.BottomCenter:
				//	break;
				//case SpriteAlignment.BottomRight:
				//	break;
				//case SpriteAlignment.Custom:
				//	break;
				default:
					screenPt1 = new Vector3(Screen.width - destHalfWidth, Screen.height - destHalfHeight, worldDistance);
					screenPt2 = new Vector3(Screen.width, Screen.height, worldDistance);
					break;
			}
			Vector3 worldPt1 = camera.ScreenToWorldPoint(screenPt1);
			Vector3 worldPt2 = camera.ScreenToWorldPoint(screenPt2);
			renderer.transform.localScale = Vector3.one;
			float worldWidth = Mathf.Abs(renderer.bounds.size.x);
			float worldHeight = Mathf.Abs(renderer.bounds.size.y);
			float worldDestWidth = Mathf.Abs(worldPt2.x - worldPt1.x) * 2;
			float worldDestHeight = Mathf.Abs(worldPt2.y - worldPt1.y) * 2;
			float scaleX = worldDestWidth / worldWidth;
			float scaleY = worldDestHeight / worldHeight;
			renderer.transform.SetParent(camera.transform, false);
			renderer.transform.position = worldPt1;
			renderer.transform.localScale = new Vector3(scaleX, scaleY, 1);
		}
	}

	public static class AnimatorUtil
	{
		public const string BaseLayerName = "Base Layer";
		public const string LayerSeparator = ".";

		public static string TriggerNameToStateName(System.Enum triggerName)
		{
			string triggerNameString = triggerName.ToString();
			return string.Concat(triggerNameString.First().ToString().ToUpper(), triggerNameString.Substring(1));
		}

		public static int EnumStringToHash(System.Enum enumString)
		{
			return Animator.StringToHash(enumString.ToString());
		}

		public static int FullPathStringToHash(System.Enum subLayerName, System.Enum stateName)
		{
			return FullPathStringToHash(subLayerName.ToString(), stateName.ToString());
		}

		public static int FullPathStringToHash(System.Enum stateName)
		{
			return FullPathStringToHash(stateName.ToString());
		}

		public static int FullPathStringToHash(params string[] subStatePathComponents)
		{
			return Animator.StringToHash(StateFullPath(subStatePathComponents));
		}

		private static string StateFullPath(params string[] subStatePathComponents)
		{
			string[] fullPathComponents = new string[subStatePathComponents.Length + 1];
			fullPathComponents[0] = BaseLayerName;
			for (int i = 0; i < subStatePathComponents.Length; i++)
			{
				fullPathComponents[i + 1] = subStatePathComponents[i];
			}
			return System.String.Join(LayerSeparator, fullPathComponents);
		}
	}

	public static class EnumUtil
	{
		//public static Array GetValues<T>()
		//{
		//	return Enum.GetValues(typeof(T));
		//}

		//public static IEnumerable<T> GetValuesEnumerable<T>() 
		//{
		//	return (T[])Enum.GetValues(typeof(T));
		//}

		public static List<string> GetNamesList<T>()
		{
			string[] names = Enum.GetNames(typeof(T));
			return new List<string>(names);
		}

		public static T[] GetValues<T>()
		{
			return (T[])Enum.GetValues(typeof(T));
		}

		public static List<T> GetList<T>()
		{
			return GetValues<T>().ToList();
		}

		public static IEnumerable<string> CastToStringEnumerable(this Enum[] array)
		{
			return array.Select(enumName => enumName.ToString());
		}

		public static List<string> CastToStringList(this Enum[] array)
		{
			return CastToStringEnumerable(array).ToList();
		}

		public static void AddEnumNamesWithPrefixToFloatDictionary<T>(Dictionary<string, float> dict, string prefix)
		{
			List<string> names = EnumUtil.GetNamesList<T>();
			foreach (var name in names)
			{
				if (name.StartsWith(prefix) && !dict.ContainsKey(name))
				{
					dict.Add(name, 0);
				}
			}
		}

		public static void AddEnumNamesWithPrefixToList<T>(List<string> list, string prefix)
		{
			List<string> names = EnumUtil.GetNamesList<T>();
			names = names.FindAll(name => name.StartsWith(prefix) && !list.Contains(name));
			list.AddRange(names);
		}

		public static void AddEnumNamesToList<T>(List<string> list)
		{
			List<string> names = EnumUtil.GetNamesList<T>();
			names = names.FindAll(name => !list.Contains(name));
			list.AddRange(names);
		}
	}

	public static class CoroutineUtil
	{
		public static void MoveThrough(this IEnumerator enumerator)
		{
			while (enumerator.MoveNext())
			{
				object current = enumerator.Current;
				if (current is IEnumerator)
				{
					(current as IEnumerator).MoveThrough();
				}
			}
		}

		public static IEnumerator ActionAfterCustomYieldInstruction(UnityAction action, CustomYieldInstruction yieldInstruction)
		{
			yield return yieldInstruction;
			if (action != null)
			{
				action.Invoke();
			}
		}

		public static IEnumerator ActionAfterTime(UnityAction action, float delayInSeconds)
		{
			yield return new WaitForSeconds(delayInSeconds);
			if (action != null)
			{
				action.Invoke();
			}
		}

		public static IEnumerator ActionAfterFrames(UnityAction action, int framesNumber)
		{
			for (int i = 0; i < framesNumber; i++)
			{
				yield return new WaitForEndOfFrame();
			}
			if (action != null)
			{
				action.Invoke();
			}
		}
	}

	public static class PathUtil
	{
		public static string GetFilePathWithoutExtension(string filepath)
		{
			int index = filepath.LastIndexOf('.');
			return index < 0 ? filepath : filepath.Substring(0, index);
		}
	}

	public static class GameObjectExtensionMethods
	{
		public static string GetSceneName(this GameObject gameObject)
		{
			return gameObject.scene.name;
		}

		public static T GetSceneName<T>(this GameObject gameObject)
		{
			return gameObject.GetSceneName().ParseToEnum<T>();
		}
	}

	public static class ComponentExtensionMethods
	{
		public static string GetSceneName(this Component component)
		{
			return component.gameObject.GetSceneName();
		}

		public static T GetSceneName<T>(this Component component)
		{
			return component.gameObject.GetSceneName<T>();
		}
	}

	public static class StringExtensionMethods
	{
		public static bool Contains(this string str, string substring, StringComparison comp)
		{
			if (substring == null)
				throw new ArgumentNullException("substring", "substring cannot be null.");
			else if (!Enum.IsDefined(typeof(StringComparison), comp))
				throw new ArgumentException("comp is not a member of StringComparison", "comp");

			return str.IndexOf(substring, comp) >= 0;
		}

		public static string TrimPrefix(this string text, string prefix)
		{
			return text.StartsWith(prefix) ? text.Substring(prefix.Length) : text;
		}

		public static string TrimSuffix(this string text, string suffix)
		{
			return text.EndsWith(suffix) ? text.Substring(0, text.Length - suffix.Length) : text;
		}

		public static bool TryParseToEnum<T>(this string name, out T enumName)
		{
			try
			{
				enumName = (T)Enum.Parse(typeof(T), name);
				return true;
			}
			catch (ArgumentException)
			{
				Debug.LogWarning("ParseToEnum: failed to parse " + name + " to " + typeof(T).ToString());
				enumName = default(T);
				return false;
			}
		}

		public static T ParseToEnum<T>(this string name)
		{
			try
			{
				return (T)Enum.Parse(typeof(T), name);
			}
			catch (ArgumentException)
			{
				Debug.LogWarning("ParseToEnum: failed to parse " + name + " to " + typeof(T).ToString());
				return default(T);
			}
		}

		public static bool EqualsToOneOfNames(this string name, params string[] array)
		{
			return array.ToList().FindIndex(item => item.Equals(name)) >= 0;
		}

		public static bool EqualsToOneOfNames<T>(this string name, params T[] array)
		{
			return array.ToList().FindIndex(item => item.ToString().Equals(name)) >= 0;
		}

		public static string RemoveWhitespace(this string input)
		{
			//return string.Join("", input.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
			return new string(input.ToCharArray().Where(c => !char.IsWhiteSpace(c)).ToArray());
		}
	}

	public static class MonoBehaviourExtensionMethods
	{
		public static Coroutine StartCoroutineActionAfterTime(this MonoBehaviour mono, UnityAction action, float delayInSeconds)
		{
			return mono.StartCoroutine(CoroutineUtil.ActionAfterTime(action, delayInSeconds));
		}
		public static Coroutine StartCoroutineActionAfterFrames(this MonoBehaviour mono, UnityAction action, int framesNumber)
		{
			return mono.StartCoroutine(CoroutineUtil.ActionAfterFrames(action, framesNumber));
		}
		public static Coroutine StartCoroutineActionAfterCustomYieldInstruction(this MonoBehaviour mono, UnityAction action, CustomYieldInstruction yieldInstruction)
		{
			return mono.StartCoroutine(CoroutineUtil.ActionAfterCustomYieldInstruction(action, yieldInstruction));
		}

	}

	public static class RectTransformExtensionMethods
	{
		public static void SetOrientationToFillParent(this RectTransform rectTransform, bool rotated, bool upsideDown, float topOffset, float bottomOffset)
		{
			if (rotated)
			{
				rectTransform.rotation = Quaternion.Euler(0, 0, upsideDown ? -90 : 90);
				rectTransform.FillRotatedParent();
				float maxVerticalOffset = Mathf.Max(topOffset, bottomOffset);
				rectTransform.offsetMin = new Vector2(maxVerticalOffset, 0);
				rectTransform.offsetMax = new Vector2(-maxVerticalOffset, 0);
			}
			else
			{
				rectTransform.rotation = Quaternion.Euler(0, 0, upsideDown ? 180 : 0);
				rectTransform.FillParent();
				rectTransform.offsetMin = new Vector2(0, bottomOffset);
				rectTransform.offsetMax = new Vector2(0, -topOffset);
			}
		}

		public static void SetOrientationToFillParent(this RectTransform rectTransform, ScreenOrientation orientation, float topOffset, float bottomOffset)
		{
			bool rotated = false;
			bool upsideDown = false;
			switch (Screen.orientation)
			{
				case ScreenOrientation.Portrait:
					rotated = orientation == ScreenOrientation.LandscapeLeft || orientation == ScreenOrientation.LandscapeRight;
					upsideDown = orientation == ScreenOrientation.PortraitUpsideDown || orientation == ScreenOrientation.LandscapeRight;
					break;
				case ScreenOrientation.PortraitUpsideDown:
					rotated = orientation == ScreenOrientation.LandscapeLeft || orientation == ScreenOrientation.LandscapeRight;
					upsideDown = orientation == ScreenOrientation.Portrait || orientation == ScreenOrientation.LandscapeLeft;
					break;
				//case ScreenOrientation.Landscape:
				case ScreenOrientation.LandscapeLeft:
					rotated = orientation == ScreenOrientation.Portrait || orientation == ScreenOrientation.PortraitUpsideDown;
					upsideDown = orientation == ScreenOrientation.LandscapeRight || orientation == ScreenOrientation.PortraitUpsideDown;
					break;
				case ScreenOrientation.LandscapeRight:
					rotated = orientation == ScreenOrientation.Portrait || orientation == ScreenOrientation.PortraitUpsideDown;
					upsideDown = orientation == ScreenOrientation.LandscapeLeft || orientation == ScreenOrientation.Portrait;
					break;
				case ScreenOrientation.AutoRotation:
				default:
					break;
			}
			rectTransform.SetOrientationToFillParent(rotated, upsideDown, topOffset, bottomOffset);
		}

		public static void FillParent(this RectTransform rectTransform)
		{
			rectTransform.anchorMin = new Vector2(0, 0);
			rectTransform.anchorMax = new Vector2(1, 1);
			rectTransform.pivot = new Vector2(0.5f, 0.5f);
			rectTransform.anchoredPosition = Vector2.zero;
			rectTransform.offsetMax = rectTransform.offsetMin = Vector2.zero;
		}

		public static void FillRotatedParent(this RectTransform rectTransform)
		{
			//Vector2 parentSize = parentTansform.rect.size;
			//rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
			//rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			//rectTransform.pivot = new Vector2(0.5f, 0.5f);
			//rectTransform.anchoredPosition = Vector2.zero;
			//rectTransform.offsetMax = rectTransform.offsetMin = Vector2.zero;
			//rectTransform.sizeDelta = new Vector2(parentSize.y, parentSize.x);

			Vector2 normalizedSize = new Vector2(ScreenUtil.AspectRatioInv, ScreenUtil.AspectRatio);
			Vector2 normalizedOffsets = new Vector2(-(normalizedSize.x - 1f) / 2f, (1f - normalizedSize.y) / 2f);

			rectTransform.anchorMin = new Vector2(normalizedOffsets.x, normalizedOffsets.y);
			rectTransform.anchorMax = new Vector2(1f - normalizedOffsets.x, 1f - normalizedOffsets.y);
			rectTransform.pivot = new Vector2(0.5f, 0.5f);
			rectTransform.anchoredPosition = Vector2.zero;
			rectTransform.offsetMax = rectTransform.offsetMin = Vector2.zero;
		}

		public static void ResetAnchoredPosition(this RectTransform rectTransform, Vector2 offsets, Vector2 pivot)
		{
			rectTransform.anchoredPosition = Vector3.zero;
			rectTransform.offsetMax = rectTransform.offsetMin = offsets;
			rectTransform.pivot = pivot;
		}

		/// <summary>
		/// rect transform into coordinates expressed as seen on the screen (in pixels)
		/// takes into account RectTrasform pivots
		/// based on answer by Tobias-Pott
		/// http://answers.unity3d.com/questions/1013011/convert-recttransform-rect-to-screen-space.html
		/// </summary>
		/// <param name="transform"></param>
		/// <param name="upsideDown"></param>
		/// <returns></returns>
		public static Rect GetScreenSpaceRect(this RectTransform transform, bool upsideDown = false)
		{
			Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
			size = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
			Rect rect;
			if (upsideDown)
			{
				rect = new Rect(transform.position.x, Screen.height - transform.position.y, size.x, size.y);
				rect.x -= (transform.pivot.x * size.x);
				rect.y -= ((1.0f - transform.pivot.y) * size.y);
			}
			else
			{
				rect = new Rect(transform.position.x, transform.position.y, size.x, size.y);
				rect.x -= (transform.pivot.x * size.x);
				rect.y -= (transform.pivot.y * size.y);
			}
			return rect;
		}

		public static Rect GetScreenSpaceRectRotated(this RectTransform transform, bool upsideDown = false)
		{
			Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
			size = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
			Rect rect;
			if (upsideDown)
			{
				rect = new Rect(transform.position.x, Screen.height - transform.position.y, size.y, size.x);
				rect.x -= ((1.0f - transform.pivot.y) * size.y);
				rect.y -= (transform.pivot.x * size.x);

			}
			else
			{
				rect = new Rect(transform.position.x, transform.position.y, size.y, size.x);
				rect.x -= (transform.pivot.y * size.y);
				rect.y -= (transform.pivot.x * size.x);
			}
			return rect;
		}

		public static bool IsInViewportSpace(this RectTransform rectTrans1, RectTransform rectTrans2, bool upsideDown = false)
		{
			Rect rect1 = rectTrans1.GetScreenSpaceRect(upsideDown);
			Rect rect2 = rectTrans2.GetScreenSpaceRect(upsideDown);

			return rect1.Overlaps(rect2);
		}
	}

	public static class SkinnedMeshRenderernExtensionMethods
	{
		public static string GetFullBlendShapeName(this SkinnedMeshRenderer renderer, string name)
		{
			return string.Concat(renderer.name, "_blendShape.", name);
		}

		public static string GetFullBlendShapeName(this SkinnedMeshRenderer renderer, System.Enum enumName)
		{
			return renderer.GetFullBlendShapeName(enumName.ToString());
		}

		public static int GetBlendShapeIndexFromShortName(this SkinnedMeshRenderer renderer, System.Enum enumName)
		{
			return renderer.GetBlendShapeIndexFromShortName(enumName.ToString());
		}

		public static int GetBlendShapeIndexFromShortName(this SkinnedMeshRenderer renderer, string name)
		{
			name = renderer.GetFullBlendShapeName(name);
			int blendShapeIndex = renderer.sharedMesh.GetBlendShapeIndex(name);
			if (blendShapeIndex < 0)
			{
				name = string.Concat(name, " ");
				blendShapeIndex = renderer.sharedMesh.GetBlendShapeIndex(name);
			}
			return blendShapeIndex;
		}

		public static void SetBlendShapeWeight(this SkinnedMeshRenderer renderer, string name, float value)
		{
			int blendShapeIndex = renderer.GetBlendShapeIndexFromShortName(name);
			if (blendShapeIndex >= 0)
			{
				renderer.SetBlendShapeWeight(blendShapeIndex, value);
			}
		}

		public static void SetBlendShapeWeight(this SkinnedMeshRenderer renderer, System.Enum enumName, float value)
		{
			//Debug.Log("SetBlendShapeWeight: renderer.name=" + renderer.name + " blendshapeName=" + enumName);
			//int testIndex = 7;
			//string blendShapeNameAtTestIndex = renderer.sharedMesh.GetBlendShapeName(testIndex);
			//int blendShapeIndexOfBlendShapeNameAtTestIndex = renderer.sharedMesh.GetBlendShapeIndex(blendShapeNameAtTestIndex);
			//Debug.Log("SetBlendShapeWeight: " + blendShapeIndexOfBlendShapeNameAtTestIndex + "*" + blendShapeNameAtTestIndex + "*" + enumName.ToString() + "*" + blendShapeNameAtTestIndex.Equals(enumName.ToString()) + "*");

			SetBlendShapeWeight(renderer, enumName.ToString(), value);
		}

		public static void PrintBonesInFlatView(this SkinnedMeshRenderer renderer, int? counter = null)
		{
			Transform[] bones = renderer.bones;

			if (counter == null)
				counter = bones.Length;

			string s = string.Concat("PrintBonesInFlatView: ", renderer.name, " bones.Length=", bones.Length, " bonesCounter=", counter, "\n");
			for (int i = 0; i < bones.Length; i++)
			{
				Transform bone = bones[i];
				if (bone != null)
				{
					if (bone.parent == null)
						s = string.Concat(s, bone.name, " ", bone.transform.localEulerAngles, "\n");
					else
						s = string.Concat(s, bone.name, " ", bone.transform.localEulerAngles, " ", bone.parent.name, "\n");
				}
				else
				{
					s = string.Concat(s, i, " bone is null\n");
				}
			}
			Debug.Log(s);
		}
	}


	public static class ListExtensionMethods
	{
		public static void RemoveAllNullElements<T>(this List<T> list)
		{
			list.RemoveAll(item => item == null);
		}

		//public static void AddArray<T>(this List<T> list, T[] array)
		//{
		//	for (int i = 0; i < array.Length; i++)
		//	{
		//		list.Add(array[i]);
		//	}
		//}
	}

	public static class TransformExtensionMethods
	{
		/// <summary>
		/// https://docs.unity3d.com/ScriptReference/Object.DestroyImmediate.html
		/// Destroys the object obj immediately. You are strongly recommended to use Destroy instead.
		/// </summary>
		/// <param name="transform"></param>
		/// <returns></returns>
		public static int DestroyAllChildrenImmediate(this Transform transform)
		{
			int ret = transform.childCount;
			for (int i = ret - 1; i >= 0; --i)
			{
				GameObject.DestroyImmediate(transform.GetChild(i).gameObject);
			}
			return ret;
		}

		public static void DestroyAllChildren(this Transform transform, Transform trash = null)
		{
			List<Transform> childrenList = new List<Transform>();
			foreach (Transform child in transform)
			{
				childrenList.Add(child);
			}
			transform.DetachChildren();
			if (trash != null)
			{
				foreach (Transform child in childrenList)
				{
					child.SetParent(trash);
					GameObject.Destroy(child.gameObject);
				}
			}
			else
			{
				childrenList.ForEach(child => GameObject.Destroy(child.gameObject));
			}
		}

		public static int GetActiveChildCount(this Transform transform)
		{
			int counter = 0;
			foreach (Transform child in transform)
			{
				if (child.gameObject.activeSelf)
					counter++;
			}
			return counter;
		}

		public static List<T> GetActiveChildren<T>(this Transform transform) where T : Component
		{
			List<T> childList = new List<T>();
			foreach (T child in transform)
			{
				if (child.gameObject.activeSelf)
					childList.Add(child);
			}
			return childList;
		}

		public static Vector3 GetAxisClosestToDirection(this Transform transfrom, Vector3 v, out float sign)
		{
			Vector3[] axis = new Vector3[] { transfrom.right, transfrom.up, transfrom.forward };
			int axisIndex = 0;
			float maxDotP = 0;
			for (int i = 0; i < 3; i++)
			{
				float dotP = Vector3.Dot(axis[i], v);
				if (Mathf.Abs(dotP) > Mathf.Abs(maxDotP))
				{
					maxDotP = dotP;
					axisIndex = i;
				}
			}
			sign = Mathf.Sign(maxDotP);
			return axis[axisIndex];
		}

		public static float GetTurnOfAxisClosestToDirection(this Transform transfrom, Vector3 v)
		{
			Vector3[] axis = new Vector3[] { transfrom.right, transfrom.up, transfrom.forward };
			float maxDotP = 0;
			for (int i = 0; i < 3; i++)
			{
				float dotP = Vector3.Dot(axis[i], v);
				if (Mathf.Abs(dotP) > Mathf.Abs(maxDotP))
				{
					maxDotP = dotP;
				}
			}
			//Debug.LogWarning("GetTurnOfAxisClosestToDirection: "+ maxDotP);
			return Mathf.Sign(maxDotP);
		}

		public static Transform FindChildInHierarchy(this Transform transform, string name)
		{
			if (transform.name.Equals(name))
				return transform;

			foreach (Transform child in transform)
			{
				Transform result = child.FindChildInHierarchy(name);
				if (result != null)
					return result;
			}
			return null;
		}

		public static bool IsInCameraView(this Transform transform, Camera camera, float worldHeight, float viewportOffsetX = 0f, float viewportOffsetY = 0f)
		{
			Vector3 p1 = transform.position;
			Vector3 p2 = p1 + transform.up * worldHeight;

			Vector3 screenP1 = camera.WorldToViewportPoint(p1);
			Vector3 screenP2 = camera.WorldToViewportPoint(p2);

			float viewportXmin = -viewportOffsetX;
			float viewportXmax = 1f + viewportOffsetX;
			float viewportYmin = -viewportOffsetY;
			float viewportYmax = 1 + viewportOffsetY;

			Rect viewportRect = Rect.MinMaxRect(viewportXmin, viewportYmin, viewportXmax, viewportYmax);

			bool isInCameraView =
				(viewportRect.Contains(screenP1) && screenP1.z > camera.nearClipPlane && screenP1.z < camera.farClipPlane) ||
				(viewportRect.Contains(screenP2) && screenP2.z > camera.nearClipPlane && screenP2.z < camera.farClipPlane);

			return isInCameraView;
		}
	}

	public static class ScrollRectExtensionMethods
	{
		public static void SetHorizontalLayoutView(this RectTransform parent, int visibleColumns, RectTransform slotRect, bool middle, bool keepContentRatio = false)
		{
			ScrollRect scrollRect = parent.GetComponent<ScrollRect>() ?? parent.GetComponentInChildren<ScrollRect>();
			HorizontalLayoutGroup grid = scrollRect.GetComponentInChildren<HorizontalLayoutGroup>();
			RectTransform viewport = scrollRect.viewport;
			RectTransform content = scrollRect.content;

			int childCount = grid.transform.GetActiveChildCount();

			float slotRatio = slotRect.rect.width / slotRect.rect.height;

			float screenRatio = (float)Screen.width / (float)Screen.height;

			if (middle)
			{
				content.anchorMin = new Vector2(0.5f, 1);
				content.anchorMax = new Vector2(0.5f, 1);
				content.pivot = new Vector2(0.5f, 1);
			}
			else
			{
				content.anchorMin = new Vector2(0, 1);
				content.anchorMax = new Vector2(0, 1);
				content.pivot = new Vector2(0, 1);
			}

			float slotWidth = (viewport.rect.width - (grid.padding.left + grid.padding.right + (visibleColumns - 1) * grid.spacing)) / visibleColumns;
			float slotHeight = slotWidth / slotRatio;

			Vector2 slotSize = new Vector2(slotWidth, slotHeight);

			float gridHeight = grid.padding.top + grid.padding.bottom + slotSize.y;

			parent.sizeDelta = new Vector2(parent.sizeDelta.x, gridHeight);

			foreach (RectTransform child in content)
			{
				child.sizeDelta = keepContentRatio ? new Vector2(child.sizeDelta.x, slotSize.y) : slotSize;
			}
		}

		public static void SetGridLayoutView(this RectTransform parent, int visibleColumns, int horizontalCapacity, bool middle)
		{
			ScrollRect scrollRect = parent.GetComponent<ScrollRect>() ?? parent.GetComponentInChildren<ScrollRect>();
			GridLayoutGroup grid = scrollRect.GetComponentInChildren<GridLayoutGroup>();
			RectTransform viewport = scrollRect.viewport;
			RectTransform content = scrollRect.content;

			int childCount = grid.transform.GetActiveChildCount();

			int columns;

			bool horizontal = childCount <= horizontalCapacity;

			float cellRatio = grid.cellSize.x / grid.cellSize.y;

			scrollRect.horizontal = horizontal;
			scrollRect.vertical = !horizontal;

			if (horizontal)
			{
				columns = childCount;

				if (middle)
				{
					content.anchorMin = new Vector2(0, 0);
					content.anchorMax = new Vector2(1, 1);
					content.pivot = new Vector2(0.5f, 1);
				}
				else
				{
					content.anchorMin = new Vector2(0, 1);
					content.anchorMax = new Vector2(0, 1);
					content.pivot = new Vector2(0, 1);
				}
			}
			else
			{
				columns = visibleColumns;

				content.anchorMin = new Vector2(0, 1);
				content.anchorMax = new Vector2(1, 1);
				content.pivot = new Vector2(0.5f, 1);
			}
			columns = Math.Max(columns, 1);

			float cellWidth = (viewport.rect.width - (grid.padding.left + grid.padding.right + (visibleColumns - 1) * grid.spacing.x)) / visibleColumns;
			float cellHeight = cellWidth / cellRatio;

			grid.cellSize = new Vector2(cellWidth, cellHeight);

			grid.constraintCount = columns;

			int rows = Math.Max(1, (childCount + columns - 1) / columns);

			float gridHeight =
				grid.padding.top + grid.padding.bottom +
				rows * (grid.cellSize.y + grid.spacing.y) - grid.spacing.y;

			//float gridMaxHeight = GetComponent<RectTransform>().rect.height - (recordingPanel.GetComponent<RectTransform>().rect.height * 0.85f + header.rect.height);
			//parentToolbar.sizeDelta = new Vector2(0, Mathf.Min(gridMaxHeight, gridHeight));
			parent.sizeDelta = new Vector2(0, gridHeight);
		}

		public static int GetChildSiblingIndexContainingPosition(this Transform content, Vector2 position)
		{
			List<RectTransform> childList = content.GetActiveChildren<RectTransform>();
			if (childList.Count > 0)
			{
				RectTransform child = childList.Find((rectTransform) => rectTransform.GetScreenSpaceRect(false).Contains(position));
				return child ? child.GetSiblingIndex() : 0;
			}
			else
			{
				return 0;
			}
		}

		public static int GetMiddleHorizontalSiblingIndexForAnchoredPosition(this ScrollRect scrollRect, Vector2 anchoredPosition)
		{
			if (scrollRect.horizontal)
			{
				RectTransform content = scrollRect.content;
				List<RectTransform> childList = content.GetActiveChildren<RectTransform>();
				int childCount = childList.Count;

				if (childCount > 0)
				{
					float offset = anchoredPosition.x - content.anchoredPosition.x;
					for (int i = 0; i < childCount; i++)
					{
						RectTransform child = childList[i];
						float childLeftCornerPos = child.anchoredPosition.x - child.pivot.x * child.rect.width;
						float childMiddlPos = childLeftCornerPos + 0.5f * child.rect.width;

						if (offset < childMiddlPos)
						{
							if (i > 0)
							{
								RectTransform prevChild = childList[i - 1];
								float prevChildMiddlePos = prevChild.anchoredPosition.x + (0.5f - prevChild.pivot.x) * prevChild.rect.width;
								return offset > prevChildMiddlePos ? i : i - 1;
							}
							else
							{
								return 0;
							}
						}
					}
					return childCount;
				}
				else
				{
					return 0;
				}
			}
			return 0;


		}

		public static bool SnapHorizontalScrollRectToGrid(this ScrollRect scrollRect, RectTransform child, bool animate, Action<bool> callback = null)
		{
			if (scrollRect.horizontal)
			{
				LayoutGroup layoutGroup = scrollRect.GetComponentInChildren<LayoutGroup>();
				RectTransform viewport = scrollRect.viewport;
				RectTransform content = scrollRect.content;
				float offsetDenom = content.rect.width - viewport.rect.width;
				float offsetNormalized = scrollRect.horizontalNormalizedPosition;
				const float epsilon = 0.01f;

				int childCount = content.GetActiveChildCount();

				Vector2 cellSize;
				Vector2 spacing;

				if (layoutGroup is GridLayoutGroup)
				{
					GridLayoutGroup grid = layoutGroup as GridLayoutGroup;
					cellSize = grid.cellSize;
					spacing = grid.spacing;
				}
				else if (layoutGroup is HorizontalLayoutGroup && childCount > 0)
				{
					HorizontalLayoutGroup row = layoutGroup as HorizontalLayoutGroup;
					cellSize = (content.GetChild(0).GetComponent<RectTransform>()).sizeDelta;
					spacing = new Vector2(row.spacing, 0);
				}
				else
				{
					return false;
				}

				float viewportHalfWidth = viewport.rect.width / 2;
				float childMiddlePos = child.anchoredPosition.x + (0.5f - child.pivot.x) * child.rect.width;

				//Debug.Log("SnapHorizontalScrollRectToGrid: " + childMiddlePos + " " + viewportHalfWidth + " " + (content.rect.width - viewportHalfWidth));
				if (childMiddlePos < viewportHalfWidth)
				{
					offsetNormalized = 0;
				}
				else if (childMiddlePos >= content.rect.width - viewportHalfWidth)
				{
					offsetNormalized = offsetDenom >= epsilon ? 1 : 0;
				}
				else if (offsetDenom >= epsilon)
				{
					float offset = childMiddlePos - viewportHalfWidth;
					offsetNormalized = offset / offsetDenom;
				}

				if (Mathf.Abs(offsetNormalized - scrollRect.horizontalNormalizedPosition) >= epsilon)
				{
					if (animate)
					{
						scrollRect.DOKill(true);
						scrollRect.DOHorizontalNormalizedPos(offsetNormalized, 0.25f).OnComplete(() => {
							if (callback != null)
								callback.Invoke(true);
						});
					}   
					else
					{
						scrollRect.horizontalNormalizedPosition = offsetNormalized;
						if (callback != null)
							callback.Invoke(true);
					}
					//Debug.Log("SnapHorizontalScrollRectToGrid: " + offsetNormalized + " " + scrollRect.horizontalNormalizedPosition);
					return true;
				}
				else if (callback != null)
					callback.Invoke(false);

				return false;
			}
			return false;
		}

		public static bool SnapHorizontalToolbarToGrid(this RectTransform toolbar, RectTransform child, bool animate, Action<bool> callback = null)
		{
			ScrollRect scrollRect = toolbar.GetComponent<ScrollRect>() ?? toolbar.GetComponentInChildren<ScrollRect>();
			return SnapHorizontalScrollRectToGrid(scrollRect, child, animate, callback);
		}
	}

	public static class DropdownExtensionMethods
	{
		public static void SetOptions<T>(this Dropdown dropdown, string firstItemTitle, Func<T, string> itemTitle, params T[] items)
		{
			List<string> titles = new List<string>();
			if (!string.IsNullOrEmpty(firstItemTitle))
				titles.Add(firstItemTitle);
			items.ToList().ForEach(item => titles.Add(itemTitle(item)));
			dropdown.ClearOptions();
			dropdown.AddOptions(titles);
		}

		public static void SetListener(this Dropdown dropdown, UnityAction<int> onValueChanged)
		{
			dropdown.onValueChanged.RemoveAllListeners();
			dropdown.onValueChanged.AddListener(onValueChanged);
		}
	}

	public static class TextExtensionMethods
	{
		public static void SetText(this Text text, string textValue)
		{
			text.text = textValue;
		}

		public static void SetActiveWithText(this Text text, string textValue)
		{
			text.text = textValue;
			text.gameObject.SetActive(!string.IsNullOrEmpty(textValue));
		}
	}

	public static class VectorExtensionMethods
	{
		public static string ToStringWithDigits(this Vector3 v, int digits)
		{
			string digitsString = "n" + digits.ToString();
			return "("
				+ (v.x >= 0f ? " " : "") + v.x.ToString(digitsString) + ","
				+ (v.y >= 0f ? " " : "") + v.y.ToString(digitsString) + ","
				+ (v.z >= 0f ? " " : "") + v.z.ToString(digitsString) +
				")";
		}

		public static string ToStringWithDigits(this Vector4 v, int digits)
		{
			string digitsString = "n" + digits.ToString();
			return "("
				+ (v.x >= 0f ? " " : "") + v.x.ToString(digitsString) + ","
				+ (v.y >= 0f ? " " : "") + v.y.ToString(digitsString) + ","
				+ (v.z >= 0f ? " " : "") + v.z.ToString(digitsString) + ","
				+ (v.w >= 0f ? " " : "") + v.w.ToString(digitsString) +
				")";
		}
	}

	public static class Matrix4x4ExtensionMethods
	{
		public static string ToStringWithDigits(this Matrix4x4 m, int digits)
		{
			return m.GetRow(0).ToStringWithDigits(digits) + "\n"
				+ m.GetRow(1).ToStringWithDigits(digits) + "\n"
				+ m.GetRow(2).ToStringWithDigits(digits) + "\n"
				+ m.GetRow(3).ToStringWithDigits(digits);
		}

		public static float GetTanHalfFovFromProjection(this Matrix4x4 m)
		{
			// assymetric: m11 = 2n/(t-b)
			// symmetric: m11 = n/t
			return m.m11 >= 0.000001f ? 1f / m.m11 : 0f;
		}

		public static float GetFovFromProjection(this Matrix4x4 m)
		{
			// assymetric: m11 = 2n/(t-b)
			// symmetric: m11 = n/t
			return Mathf.Atan2(1f, m.m11) * 2f * Mathf.Rad2Deg;
		}
	}

	public static class CameraExtensionMethods
	{
		public static float GetTanHalfFovFromProjectionMatrix(this Camera camera)
		{
			return camera.projectionMatrix.GetTanHalfFovFromProjection();
		}

		public static float GetFovFromProjectionMatrix(this Camera camera)
		{
			return camera.projectionMatrix.GetFovFromProjection();
		}
	}

}