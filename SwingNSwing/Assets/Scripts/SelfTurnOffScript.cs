using UnityEngine;
using System.Collections;

public class SelfTurnOffScript : MonoBehaviour {

	// Use this for initialization
	void Start () {
	}
	void OnEnable(){

		Invoke("TurnOff", 3f);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	void TurnOff(){
		gameObject.SetActive (false);
	}
}
