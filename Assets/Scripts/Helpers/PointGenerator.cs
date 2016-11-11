using UnityEngine;

namespace Climbing {

	[ExecuteInEditMode]
	public class PointGenerator : MonoBehaviour {
		[HeaderAttribute("Helper properties")]
		public bool dismountPoint;
		public bool fallPoint;
		public bool hangingPoints;
		public bool singlePoint;

		[HeaderAttribute("Yup")]
		public bool updatePoints;

		[HeaderAttribute("Helpers")]
		public bool deleteAll;
		public bool createIndicators;

		
	} 
}