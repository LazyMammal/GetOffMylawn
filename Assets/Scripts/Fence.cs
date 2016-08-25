using UnityEngine;

public class Fence : MonoBehaviour 
{
	public int fencePoints = 50;
	private float nextTime = 0f, delay = 1f;
	private AudioSource successSound;
    void Start () 
	{
    	successSound = GetComponent<AudioSource>();
    }
	void OnTriggerEnter (Collider other) 
	{
		float rot = transform.parent.rotation.eulerAngles.x;
		// rot != 0f &&
		if( Time.time > nextTime && other.CompareTag("Water")  )
		{
			Debug.Log( " rot : " + rot );

			nextTime = Time.time + delay;

			// dampen water particle
			//Destroy(other.gameObject);
			other.attachedRigidbody.velocity = Vector3.zero;

			// increase player score
			GameManager.Instance.AddScore(fencePoints);

			successSound.Play();
		}
	}
}
