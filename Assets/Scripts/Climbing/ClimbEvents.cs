using UnityEngine;
using System.Collections;

namespace Climbing {

	public class ClimbEvents : MonoBehaviour {
		ClimbBehaviour _cb;

		void Start(){
			_cb = transform.root.GetComponentInChildren<ClimbBehaviour>();
		}

		public void EnableRootMovement(float t){
			StartCoroutine(Enable(t));
		}

		IEnumerator Enable(float t){
			yield return new WaitForSeconds(t);
			_cb.enableRootMovement = true;
		}
	}

}