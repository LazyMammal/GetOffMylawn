using UnityEngine;
using System.Collections;

public class TimeKill : MonoBehaviour {

	public float lifetime = 2f;

	void Start ()
	{
		Destroy (gameObject, lifetime);
	}
}
