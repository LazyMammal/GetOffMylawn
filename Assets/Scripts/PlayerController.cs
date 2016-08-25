using UnityEngine;
 
public class PlayerController : MonoBehaviour
{
	// water spray
	public GameObject projectile, aimTarget;
	public float fireRate = 10f, audioLag = .5f;
	public float randAngle = 0.1f;
	private float nextFire = 0f;
	private AudioSource sprayAudio;

	// speed is the rate at which the object will rotate
	public float rotSpeed = 250f;
	public Vector2 minAngle = new Vector2( -90f, -90f);
	public Vector2 maxAngle = new Vector2(  90f,  90f);
	public Vector2 startAngle = new Vector2( 45f,  0f);

	void Start()
	{
		sprayAudio = GetComponent<AudioSource>();
		sprayAudio.volume = 0f;
	}

	void FixedUpdate () 
	{
		// mouse ray to get hit point in world
		RaycastHit allHit; //, groundHit;
   		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); 
   		bool allFlag = Physics.Raycast (ray, out allHit);

		//int layerMask = 1 << 8;
   		//bool groundFlag = Physics.Raycast (ray, out groundHit, Mathf.Infinity, layerMask);
		//Debug.DrawLine( groundHit.point, groundHit.point + Vector3.up * 2, Color.black );

		if( allFlag )
		{
			// reticle location
			Debug.DrawLine( allHit.point, allHit.point + Vector3.up * 2, Color.blue );
			aimTarget.transform.position = allHit.point - ray.direction *.15f;

			if( allHit.point.y > .11f )
			{
				Vector3 fwd = allHit.collider.gameObject.transform.forward;
				Debug.DrawLine(allHit.point, allHit.point + fwd * 2, Color.yellow);
				aimTarget.transform.rotation = Quaternion.FromToRotation( Vector3.up, -fwd );
			}
			else
				aimTarget.transform.rotation = Quaternion.identity;
		}

		// ballistic calculation
		Debug.DrawLine( transform.position, aimTarget.transform.position, Color.black );
		Vector3 diff = aimTarget.transform.position - transform.position;
		float dist = diff.magnitude;

		// calculate target rotation
		float speed = 12.5f;
		float g = 9.81f; 			// gravity
		float vel2 = speed * speed; // velocity^2
		float vel4 = vel2 * vel2;	// velocity^4
		float aimAngle = 45f; // aim angle (above horizontal)

		// calculate angles
		float formula = Mathf.Sqrt(vel4 - g * (g * dist * dist + 2 * diff.y * vel2));
		if(!System.Single.IsNaN( formula ))
		{
			float angle1 = Mathf.Atan2( vel2 - formula, g * dist ) * Mathf.Rad2Deg;
			//Debug.Log("angle 1: " + angle1 );
			//float angle2 = Mathf.Atan2( vel2 + formula, g * dist ) * Mathf.Rad2Deg;
			//Debug.Log("angle 2: " + angle2 );
			aimAngle = angle1;
		}
		//Debug.Log("aim angle: " + aimAngle);

		Vector3 cross = Vector3.Cross(diff, Vector3.up);
		Vector3 aimVector = Quaternion.AngleAxis( aimAngle, cross ) * diff;

		aimVector.Normalize();
		Debug.DrawRay( transform.position, aimVector, Color.red );
		Quaternion targetRotation = Quaternion.LookRotation( aimVector, Vector3.up );

		/*
		// mouse screen position (as percentage)
		float mouseX = Input.mousePosition.x / Screen.width;
		float mouseY = 1f - Input.mousePosition.y / Screen.height; 	// invert mouse Y-axis

		// rotate nozzle
		Quaternion targetRotation = Quaternion.Euler(
			Mathf.Lerp( minAngle.y, maxAngle.y, mouseY) + startAngle.y,
			Mathf.Lerp( minAngle.x, maxAngle.x, mouseX) + startAngle.x,
			0f
		);
		*/

		// smooth the rotation
		float step = rotSpeed * Time.deltaTime;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, step);

		GameManager.Instance.UpdateAngles(transform.eulerAngles.x, transform.eulerAngles.y);

		// spray
		if (Input.GetButton("Fire1") )
		{
			// increase audio volume
			sprayAudio.volume = Mathf.Clamp( sprayAudio.volume + Time.deltaTime / audioLag, 0f, 1f );

			// stutter the water drops
			if( Time.time > nextFire)
			{
				nextFire = Time.time + 1f / fireRate;

				Quaternion randrot = Quaternion.Euler(
					Random.Range( -randAngle/2, randAngle/2 ),
					Random.Range( -randAngle, randAngle ),
					Random.Range( -randAngle, randAngle )
				) ;

				GameObject clone = Instantiate(projectile, transform.position, transform.rotation * randrot) as GameObject;  
			}
		}
		else 
		{
			// decrease audio volume
			sprayAudio.volume = Mathf.Clamp( sprayAudio.volume - Time.deltaTime / audioLag, 0f, 1f );
		}


	}
}

