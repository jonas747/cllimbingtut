using UnityEngine;

public class FunTest : MonoBehaviour{
	void Start(){
		Debug.Log((int)AvatarIKGoal.LeftFoot);
		Debug.Log((int)AvatarIKGoal.RightFoot);
		Debug.Log((int)AvatarIKGoal.LeftHand);
		Debug.Log((int)AvatarIKGoal.RightHand);
	}
}