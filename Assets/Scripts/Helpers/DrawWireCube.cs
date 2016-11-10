#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace Climbing {
	[ExecuteInEditMode]
	public class DrawWireCube : MonoBehaviour{
		public List<IKPosition> ikPos = new List<IKPosition>();

		public bool refresh;
		
		void Update(){
			if(refresh){
				ikPos.Clear();
				refresh = false;
			}
		}

	}
}

#endif