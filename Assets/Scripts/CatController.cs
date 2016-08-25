using UnityEngine;

public class CatController : MonoBehaviour 
{
	// health
	public int maxHealth = 10;
	private int health = 10;
	public int waterDmg = 5;
	public int catPoints = 10;

	// behaviour ...

	public float jumpChance = .1f;
	public float walkSpeed = 1.5f;
	public float runSpeed = 6f;

	public float fenceDelay = 1.5f;
	public float wetDelay = 1f;
	public float hissDelay = 2.5f;
	public float digDelay = 1.5f;

	// state: explore, dig, wet, hiss/run
	private float nextStateTime = 0f;
	private int state = -1;	// walk along fence

	// targetPoint: fence coord, garden coord
	private Vector3 targetPoint = Vector3.zero;

	private Animator animator;
	private GameObject deadFlower, digFlower;

	// audio
	public float minPitch = .7f, maxPitch = 1.3f;
	private AudioSource angrySound;

    void Start () 
	{
        animator = GetComponent<Animator>();
		//deadFlower = GameObject.Find("FlowerDead");
		deadFlower = (GameObject) Resources.Load("Prefabs/FlowerDead");

		health = maxHealth;

		angrySound = GetComponent<AudioSource>();
    }

	// check for incoming water
	void OnTriggerEnter (Collider other) 
	{
		if( other.CompareTag("Water") )
		{
			// dampen water particle
			//Destroy(other.gameObject);
			other.attachedRigidbody.velocity = Vector3.zero;

			if( Time.time > nextStateTime || state <= 1 ) // if explore/dig or time
			{
				// change state to 'wet'
				state = 2;	// wet
				setStateDelay( wetDelay );

				// flip up in air

				//animator.SetTrigger("jump");
				animator.SetBool("isJumping", true);

				angrySound.pitch = Random.Range(minPitch, maxPitch);
				angrySound.Play();

				// reduce health points
				health -= waterDmg;

				// increase player score
				GameManager.Instance.AddScore(catPoints);
			}
		}
		else if( other.tag == "Plant" && state != 3 ) //|| hasTag( other.transform, "Plant") )
		{
			// change state
			state = 1; 	// dig
			setStateDelay( digDelay );

			// reset 'wet' state
			animator.SetBool("isJumping", false);
			health = maxHealth;

			// store reference to Flower object
			digFlower = other.gameObject;

			// set move target to flower location
			targetPoint = digFlower.transform.position;
		}
	}

	// called every physics update
	void FixedUpdate()
	{
		// default move speed
		float moveSpeed = walkSpeed;

		// check health
		if( health <= 0 && state != 3) // not already hiss/run
		{
			// increase score
			GameManager.Instance.AddScore(catPoints);

			// change state to 'hiss/run'
			state = 3; // hiss
			setStateDelay( hissDelay );

			//moveSpeed = runSpeed;

			// run off to the right
			targetPoint = new Vector3( 5f, 0f, -10f );

			// or the left
			if( transform.position.x < transform.position.z )
				targetPoint = new Vector3( -10f, 0f, 5f );
		}

		// check state
		if( state == 1 ) // dig
		{
			// time is up, switch to explore
			if( Time.time > nextStateTime || targetPoint == Vector3.zero )
				state = 0;
			else 
			{
				Vector3 eps = targetPoint - transform.position; 
				if( eps.magnitude < 0.1f && digFlower != null && digFlower.activeInHierarchy)
				{
					// trigger dig animation
					// TODO

					// deactivate flower
					digFlower.SetActive( false );

					// replace with dead flower
					if( deadFlower != null )
					{
						GameObject plant = Instantiate(deadFlower, digFlower.transform.position, digFlower.transform.rotation) as GameObject;
						plant.transform.SetParent(digFlower.transform.parent);
					}

					// count remaining flowers
					GameManager.Instance.UpdateCount();

				}
			}
		}

		if( state == 2 ) // wet
		{
			// still time, walk around randomly
			if( Time.time < nextStateTime)
			{
				Vector3 eps = targetPoint - transform.position; 
				if( eps.magnitude < 0.1f || targetPoint == Vector3.zero )
				{
					GoToRandomPoint();
				}

				// skip normal target movement
				return;
			}
			else state = 0; // go explore
		}

		if( state == -1 ) // walking along fence
		{
			if( Time.time > nextStateTime )
			{
				bool isFencePossible = false;

				// test if we can walk to next fence
				Vector3 pos = transform.position + transform.forward + Vector3.up;
					
				// raycast to ground
				RaycastHit hit;
				Physics.Raycast(pos, -Vector3.up, out hit);

				Debug.DrawRay(pos, -Vector3.up, Color.red, 1f);

				if( hasTag( hit.transform, "Fence" ) )
				{
					targetPoint = hit.point;
					setStateDelay(fenceDelay);
					isFencePossible = true;
				}

				if( !isFencePossible || Random.Range(0f, 1f) < jumpChance )  // hop down
				{
					HopDown();
				}
			}
		}

		// continue with explore
		if( state == 0 ) // explore
		{
			if( Time.time > nextStateTime )
			{
				// look around, pick a new targetPoint square
				Vector3 eps = targetPoint - transform.position; 
				if( eps.magnitude < 0.1f || targetPoint == Vector3.zero )
				{
					GoToRandomPoint();
				}
			}
		}

		if( state == 3 )
			moveSpeed = runSpeed;

		if( targetPoint != Vector3.zero )
		{
			// move towards target point
			transform.position = Vector3.MoveTowards(transform.position, targetPoint, moveSpeed * Time.deltaTime);

			// get direction to target point
			Quaternion targetRotation = Quaternion.LookRotation(targetPoint - transform.position + transform.forward - Vector3.Project( transform.forward, Vector3.up ), Vector3.up);

			// Smoothly rotate towards the target point
			transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 200f * moveSpeed * Time.deltaTime);
		}		
	}

	void GoToRandomPoint()
	{
		if( transform.position.y > .5f )
		{
			HopDown();
			return;
		}

		// random pos in backyard
		Vector3 pos = new Vector3(
			Random.Range( 0f, 6.5f), 
			2f,
			Random.Range( 0f, 6.5f)
		);
		
		// raycast to ground
		RaycastHit hit;
		Physics.Raycast(pos, -Vector3.up, out hit);
		targetPoint = hit.point;
		targetPoint.y = Mathf.Clamp( targetPoint.y, 0f, .1f);
	}

	void HopDown()
	{
		// jump to nearest patch of backyard (towards bottom of screen)
		Vector3 pos = Vector3.MoveTowards(transform.position, transform.position - Vector3.right - Vector3.forward, 1f); 

		// raycast to ground
		RaycastHit hit;
		Physics.Raycast(pos, -Vector3.up, out hit);
		targetPoint = hit.point;
		targetPoint.y = Mathf.Clamp( targetPoint.y, 0f, .1f);

		// start exploring
		state = 0;
		setStateDelay( fenceDelay );

	}
	// check self or parent for tag
	bool hasTag(Transform t, string tag)
	{
		while (true)
		{
			if (t.tag == tag)
				return true;

			if( t.parent == null )
				return false;

			t = t.parent.transform;
		}
		//return false; // Could not find a parent with given tag.
	}

	void setStateDelay( float delay )
	{
		nextStateTime = Time.time + delay * Random.Range(.9f, 1.1f);
	}

}
