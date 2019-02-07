using UnityEngine;

namespace DC
{
	public class ActiveOnEnable : MonoBehaviour
	{
		private void OnDisable()
		{
			gameObject.SetActive(false);
		}

		private void OnEnable()
		{
			gameObject.SetActive(true);
		}
	}
}