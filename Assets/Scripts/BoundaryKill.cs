using UnityEngine;
using System.Collections;

public class BoundaryKill : MonoBehaviour 
{
	void OnTriggerExit (Collider other) 
	{
		Destroy(other.gameObject);
	}
}
