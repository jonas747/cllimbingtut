#if UNITY_EDITOR
using UnityEngine;

namespace Climbing{
	
	[ExecuteInEditMode]
	public class DrawLineIndividual : MonoBehaviour {
		public Point point;

		void Update(){
			if(point == null){
				point = GetComponent<Point>();

				if(point == null){
					Debug.LogError("No point", gameObject);
				}
			}
		}
	}
}

#endif