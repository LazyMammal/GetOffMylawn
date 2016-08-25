using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameManager : Singleton<GameManager> {
	// singleton pattern
	protected GameManager () {} // guarantee this will be always a singleton only - can't use the constructor!

	// Game Score
	public Text scoreText, inclineText, angleText, gardenText, finalScoreText;
	public Transform canvasPage, infoPage, gameOverPage, menuPage;
	private static int score = 0, incline = 0, angle = 0;
	private static int flowerCount = 0;

	static bool infoPageFlag = false, gameOverPageFlag = false, menuPageFlag = false;

	// board setup
	public float plantAngle = 15f;

	// cats
	public GameObject catModel;

	public float catTimer = 2f, fenceTimer = 10f;
	private float nextCat = 0f, nextFence = 0f;

	void Update()
	{
		if( scoreText != null )
			scoreText.text = "Score: " + score;

		if( gardenText != null )
			gardenText.text = "Gardens: " + flowerCount;

		if( inclineText != null )
			inclineText.text = "Incline: " + incline;

		if( angleText != null )
			angleText.text = "  Angle: " + angle;

		if( Time.time > nextCat )
		{
			nextCat = Time.time + catTimer;
			SpawnCat();
		}

		if( Time.time > nextFence )
		{
			nextFence = Time.time + fenceTimer;
			FlipFence();
		}
	
		if( Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Backspace) )
		{
			// toggle menu
			ShowMenuPage();			
		}

		if( Input.GetKeyDown(KeyCode.Slash) ) //Input.GetKeyDown("?")
		{
			// toggle info page
			ShowInfoPage();			
		}
	}
	void FlipFence( )
	{
		// get random fence (main fence only)
		GameObject bonusFence = activeFenceList[Random.Range(fenceObjWing.Count, activeFenceList.Count)];
		bonusFence.GetComponent<Animator>().SetTrigger("flip");
	}

	void SpawnCat( )
	{
		// get random fence
		GameObject fence = activeFenceList[Random.Range(0, activeFenceList.Count)];

		Quaternion rot = fence.transform.rotation * Quaternion.Euler(0f, 90f, 0f);

		SpawnCat( fence.transform.position + Vector3.up, rot );
	}

	void SpawnCat( Vector3 pos, Quaternion rot)
	{
		// raycast to ground
		RaycastHit hit;
		if (Physics.Raycast(pos + Vector3.up, -Vector3.up, out hit))
		{
			GameObject cat = Instantiate( catModel, hit.point, rot) as GameObject;
			
			SetColorAll(cat,
				Random.Range(0f, 1f),
				Random.Range(0f, 1f),
				Random.Range(0f, 1f)
			);
		}
	}

	public void AddScore(int points)
	{
		score += points;
	}

	public void UpdateAngles(float i, float a)
	{
		incline = (360 - (int)i + 180) % 360 - 180;
		angle = ((int)a - 45 + 180) % 360 - 180;
	}

	void Start()
	{
		// Setup New Board
		bool success = InitBoard();
		if( success )
		{
			// hide canvas overlays
			HideCanvas(menuPage, 0f);
			menuPageFlag = false;
			HideCanvas(infoPage, 0f);
			infoPageFlag = false;
			HideCanvas( gameOverPage, 0f );
			gameOverPageFlag = false;
			ShowCanvas( canvasPage, 0f );

			SetupBoard();
		}
	}

	public GameObject gardenModel, fenceModel, plantModel;
	public int sizeX = 7, sizeY = 7;
	int numCells = 7 * 7;

	List<int> gardenFlags = null, fenceFlags = null;
	List<GameObject> gardenObjList, flowerObjList, fenceObjLeft, fenceObjRight, fenceObjWing;
	List<GameObject> activeGardenList, activeFenceList;

	// initialize board will all pieces (will be de-activated)
	bool InitBoard()
	{
		// init board arrays to zero
		numCells = sizeX * sizeY;

		if( gardenFlags != null)
			return false;

		gardenFlags = new List<int>(numCells);
		fenceFlags = new List<int>(numCells);
		gardenObjList = new List<GameObject>(numCells);
		flowerObjList = new List<GameObject>(numCells);
		fenceObjLeft  = new List<GameObject>(numCells);
		fenceObjRight = new List<GameObject>(numCells);
		fenceObjWing = new List<GameObject>();
		activeGardenList = new List<GameObject>();
		activeFenceList = new List<GameObject>();
		
		for(int i = 0; i < numCells; i++ )
		{
			gardenFlags.Add(0);
			fenceFlags.Add(0);

			int x = i % sizeX;
			int y = i / sizeX;

			AddGarden( x, y );
			AddFence( x, y, 1 );
			AddFence( x, y, 2 );
		}

		// extra fences in 'wing' areas
		// AddFence( sizeX, sizeY, 1 );

		AddFence( sizeX-1, -1, 2, true );
		AddFence( sizeX-1, -2, 2, true );
		AddFence( sizeX,   -3, 1, true );
		AddFence( sizeX+1, -3, 1, true );

		AddFence( -1, sizeY-1, 1, true );
		AddFence( -2, sizeY-1, 1, true );
		AddFence( -3, sizeY,   2, true );
		AddFence( -3, sizeY+1, 2, true );

		return true;
	}

	// setup board for a new game
	void SetupBoard()	
	{
		// destroy cats
		GameObject[] objArr = GameObject.FindGameObjectsWithTag("Cat");

		foreach (GameObject obj in objArr)
			Destroy(obj);

		// activate Flowers (nested in gardens)
		foreach( GameObject go in flowerObjList )
		{
			go.SetActive(true);
		}

		// hide: fences, gardens
		HideByTag("Garden");
		activeGardenList.Clear();
		flowerCount = 0;

		HideByTag("Fence");
		activeFenceList.Clear();

		// show "wing" fences
		foreach( GameObject go in fenceObjWing )
		{
			ResetFence(go);
			activeFenceList.Add(go);
		}

		// init board array to zero
		for(int i = 0; i < numCells; i++ )
		{
			gardenFlags[i] = 0;
			fenceFlags[i] = 0;
		}

		// TODO: set difficulty according to level
		int playerLevel = 3;
		int numGardens = 12;

		// populate gardens
		for(int i = 0; i < numGardens; i++ )
		{
			int idx = Random.Range(0, numCells);

			if( idx < gardenFlags.Count && idx < gardenObjList.Count && gardenFlags[idx] == 0 )
			{
				gardenFlags[idx] = 1;
				gardenObjList[idx].SetActive(true);
				activeGardenList.Add(gardenObjList[idx]);
				flowerCount++;
			}
		}

		// place fence along "back" edges (x,Y-1) and (X-1,y)
		for(int i = 0; i < sizeX; i++ )
		{
			int idx = getIndex(i,sizeY-1);
	
			if( idx < fenceFlags.Count )
				fenceFlags[idx] += 1;
	
			if( idx < fenceObjLeft.Count )
			{
				ResetFence( fenceObjLeft[idx] );
				activeFenceList.Add(fenceObjLeft[idx]);
			}
		}

		for(int i = 0; i < sizeY; i++ )
		{
			int idx = getIndex(sizeX-1, i);

			if( idx < fenceFlags.Count )
				fenceFlags[idx] += 2;
	
			if( idx < fenceObjRight.Count )
			{
				ResetFence( fenceObjRight[idx] );
				activeFenceList.Add(fenceObjRight[idx]);
			}
		}
	}

	Vector3 GetBoardCoord(int x, int y)
	{
		// (-1,-1) is HOSE LOCATION
		// ( 0, 0) is BOTTOM CENTRE +1,+1 from HOSE

		return new Vector3(
			x + .5f,
			0f,
			y + .5f
		);
	}

	void AddGarden( int x, int y )
	{
		// get coordinates for garden plot
		Vector3 target = GetBoardCoord(x, y);

		// create garden object
		if( gardenModel != null && plantModel != null)
		{
			GameObject garden = Instantiate(gardenModel, target, Quaternion.identity) as GameObject;

			Quaternion plantRotation = Quaternion.Euler(
				-Random.Range( 0f, plantAngle ),
				-Random.Range( 0f, plantAngle ),
				-Random.Range( 0f, plantAngle )
			);

			GameObject plant = Instantiate(plantModel, target + Vector3.up * .1f, plantRotation) as GameObject;
			plant.transform.SetParent(garden.transform);

			gardenObjList.Add(garden);
			flowerObjList.Add(plant);
		}
	}

	void AddFence( int x, int y, int t, bool wings = false )
	{
		// get coordinates for fence plot
		Vector3 target = GetBoardCoord(x, y);
		Quaternion targetRot = Quaternion.identity;

		if( t == 1 )
		{
			target.z += .5f;
		}
		else if ( t == 2 )
		{
			target.x += .5f;
			targetRot = Quaternion.Euler( 0f, 90f, 0f);
		}

		// create fence objects
		if( fenceModel != null)
		{
			GameObject fence = Instantiate(fenceModel, target, targetRot) as GameObject;
			if( wings )
			{
				fenceObjWing.Add( fence );
			}
			else if( t == 1 )
			{
				fenceObjLeft.Add( fence );
			}
			else if ( t == 2 )
			{
				fenceObjRight.Add( fence );
			}

		}
	}

	int getIndex(int x, int y)
	{
		if( x < 0 || y < 0 || x >= sizeX || y >= sizeY )
			return -1;
		return (x + y * sizeX);
	}

	void HideByTag(string tag, bool active = false)
	{
		GameObject[] objArr = GameObject.FindGameObjectsWithTag(tag);

		foreach (GameObject obj in objArr)
			obj.SetActive( active );
	}

	void SetColorAll(GameObject go, float r, float g, float b )
	{
		Component[] arrayRenderer;
        arrayRenderer = go.GetComponentsInChildren<Renderer>( );

        foreach( Renderer render in arrayRenderer )
            render.material.color = new Color(r,g,b);
	}

	public void UpdateCount()
	{
		GameObject[] objArr = GameObject.FindGameObjectsWithTag("Plant");
		flowerCount = objArr.Length;

		if( flowerCount == 0 )
		{
			Debug.Log("Game Over");

			// capture final score
			if( finalScoreText != null )
				finalScoreText.text = "Score: " + score;

			if( infoPageFlag )
			{
				HideCanvas(infoPage);
				infoPageFlag = false;
			}
			else HideCanvas(infoPage, 0f);

			if( menuPageFlag )
			{
				HideCanvas(menuPage);
				menuPageFlag = false;
			}
			else HideCanvas(menuPage, 0f);
			
			HideCanvas( canvasPage );
			ShowCanvas( gameOverPage );
			gameOverPageFlag = true;
		}
	}

	public void NewGame()
	{
		score = 0;

		if( infoPageFlag )
		{
			HideCanvas(infoPage);
			infoPageFlag = false;
		}
		else HideCanvas(infoPage, 0f);

		if( menuPageFlag )
		{
			HideCanvas(menuPage);
			menuPageFlag = false;
		}
		else HideCanvas(menuPage, 0f);

		HideCanvas( gameOverPage );
		gameOverPageFlag = false;

		ShowCanvas( canvasPage );

		SetupBoard();
	}

	void HideCanvas(Transform tr, float duration = .25f)
	{
		CanvasGroup cg = tr.GetComponent<CanvasGroup>();
		if( cg != null )
		{
			cg.interactable = false;
			cg.blocksRaycasts = false;
			if( duration > 0f )
				StartCoroutine(FadeCG(cg, 0f, duration));
			else
				cg.alpha = 0f;
		}
	}

	void ShowCanvas(Transform tr, float duration = .25f)
	{
		CanvasGroup cg = tr.GetComponent<CanvasGroup>();
		if( cg != null )
		{
			cg.interactable = true;
			cg.blocksRaycasts = true;
			if( duration > 0f )
				StartCoroutine(FadeCG(cg, 1f, duration));
			else
				cg.alpha = 1f;
		}
	}

	public void ShowMenuPage()
	{
		if( menuPageFlag )
		{
			HideCanvas(infoPage); //, 0f);
			infoPageFlag = false;

			HideCanvas(menuPage);
			menuPageFlag = false;

			if( gameOverPageFlag )
				ShowCanvas(gameOverPage);
			else HideCanvas(gameOverPage, 0f);
		}
		else
		{
			HideCanvas(infoPage); //, 0f);
			infoPageFlag = false;

			ShowCanvas(menuPage);
			menuPageFlag = true;

			if( gameOverPageFlag )
				HideCanvas(gameOverPage);
			else HideCanvas(gameOverPage, 0f);
		}
	}

	public void ShowInfoPage()
	{
		if( infoPageFlag )
		{
			HideCanvas(menuPage); //, 0f);
			menuPageFlag = false;

			HideCanvas(infoPage);
			infoPageFlag = false;

			if( gameOverPageFlag )
				ShowCanvas(gameOverPage);
			else HideCanvas(gameOverPage, 0f);
		}
		else
		{
			HideCanvas(menuPage); //, 0f);
			menuPageFlag = false;

			ShowCanvas(infoPage);
			infoPageFlag = true;

			if( gameOverPageFlag )
				HideCanvas(gameOverPage);
			else HideCanvas(gameOverPage, 0f);
		}
	}

	IEnumerator FadeCG(CanvasGroup cg, float alpha = 1f, float time = 1f)
    {
		int steps = 100;
		float stepSize = (alpha - cg.alpha) / time / (float)steps;
        for(int i = 0; i < steps; i++)
        {
            cg.alpha += stepSize;
            yield return new WaitForSeconds(time / (float) steps);
        }
    }

	void ResetFence(GameObject go)
	{
		//go.GetComponent<Animator>().SetTrigger("reset");
		go.GetComponent<Animator>().Rebind();
		go.SetActive(true);
	}
	public void ExitGame()
	{
		Application.Quit();
	}
}
