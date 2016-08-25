using UnityEngine;

public class Mover_rand : MonoBehaviour {

	public float speed = 10f;
	public Vector3 minVelocity = new Vector3( -1f, -1f, -1f );
	public Vector3 maxVelocity = new Vector3(  1f,  1f,  1f ); 

	void Start ()
	{
		Component[] arrayRigidbody;
        arrayRigidbody = GetComponentsInChildren<Rigidbody>( );

        foreach( Rigidbody rb in arrayRigidbody )
			rb.velocity = speed * new Vector3(
				Random.Range(minVelocity.x, maxVelocity.x),
				Random.Range(minVelocity.y, maxVelocity.y),
				Random.Range(minVelocity.z, maxVelocity.z)
			);
	}
}
