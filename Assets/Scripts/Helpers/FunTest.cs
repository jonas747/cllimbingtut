using UnityEngine;

public class FunTest : MonoBehaviour{
	public bool onAir;

	public int hash;

	public bool isOnAir;
	public bool isLedge;
	public bool isUp;
	public bool isSplitIdle;

	void Update(){
		var anim = GetComponent<Animator>();

		var curState = anim.GetCurrentAnimatorStateInfo(0);

		isOnAir = curState.IsName("onAir");
		isLedge = curState.IsTag("Ledge");
		isUp = curState.IsTag("Up");
		isSplitIdle = curState.IsTag("SplitIdle");
		
		hash = curState.fullPathHash;

		onAir = anim.GetBool("onAir");
	}
}