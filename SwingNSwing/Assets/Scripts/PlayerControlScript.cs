using UnityEngine;
using System.Collections;
using XInputDotNetPure; // Required in C#

public class PlayerControlScript : MonoBehaviour {

	public Rigidbody2D RB;
	public float groundSpeed, jumpHeight, DashSpeed;
	public GameObject grappleAnchor;
	public GameObject swingEffect;
	public int playerNumber;
	float xStick, yStick, deadSize = .25f;
	bool inputEnabled = true, doubleJump = false, grounded = false, onWallRight = false, onWallLeft = false, leftRightEnabled = true, swinging = false, facingRight, dash = false, canAttack = true;
	ScoreScript SS;

	int groundMask, playerGroundMask;
	int groundedBuffer = 0, wallBuffer = 0;
	Vector2 swingPoint, grappleDirection;
	Vector3 prevPosition;
	float SwingRadius;
	public LineRenderer LR;
	SpriteRenderer SR;
	//ObjectPoolScript SwingEffectPool;

	PlayerIndex playerIndex;
	GamePadState state;
	GamePadState prevState;
	// Use this for initialization
	void Start () {
		groundMask = 1 << 8;
		playerGroundMask = 1 << 9; // maybe nine maybe just a number
		playerIndex = (PlayerIndex)playerNumber;
		SR = GetComponent<SpriteRenderer> ();
		SS = GameObject.Find ("ScoreObject").GetComponent<ScoreScript>();
		//SwingEffectPool = GameObject.Find ("LinePooler").GetComponent<ObjectPoolScript> ();
	}
	
	// Update is called once per frame
	void FixedUpdate () {

		prevState = state;
		state = GamePad.GetState(playerIndex, GamePadDeadZone.None);

		// block for checking ground
		if (!grounded) {
			// do a check for ground
			RaycastHit2D groundCheck = Physics2D.Raycast (transform.position, Vector2.down, .35f, groundMask);
			if (groundCheck.collider != null && groundedBuffer <= 0) {
				grounded = true;
				doubleJump = true;
				dash = true;
			} else if (groundedBuffer > 0) {
				groundedBuffer--;
			}

			// do a check for a wall hang
			RaycastHit2D wallCheckRight = Physics2D.Raycast (transform.position, Vector2.right, .35f, groundMask);
			RaycastHit2D wallCheckLeft	= Physics2D.Raycast (transform.position, Vector2.left, .35f, groundMask);
			if (wallCheckRight.collider != null && wallBuffer <= 0) {
				onWallRight = true;
				doubleJump = true;
				if (swinging) {
					BreakLine ();
				}
			} else if (wallCheckLeft.collider != null && wallBuffer <= 0) {				 
				onWallLeft = true;
				doubleJump = true;
				if (swinging) {
					BreakLine ();
				}
			} else if (wallBuffer > 0) {
				wallBuffer--;
			} else if (wallCheckLeft.collider == null && wallCheckRight.collider == null && (onWallLeft || onWallRight)) {
				onWallLeft = false;
				onWallRight = false;
			}
		} else {
			onWallRight = false;
			onWallLeft = false;
		}


		if (inputEnabled) {
			// axis crontrols for horizontal movement
			if (Mathf.Abs (state.ThumbSticks.Left.X) > deadSize && !onWallRight && !onWallLeft && leftRightEnabled) {
				xStick = state.ThumbSticks.Left.X;
			} else if (grounded) {
				xStick = 0;
			} else {
				xStick = RB.velocity.x / groundSpeed;
			}

			if (xStick > 0) {
				facingRight = true;
			} else if (xStick < 0) {
				facingRight = false;
			}

			RB.velocity = new Vector3 (xStick * groundSpeed, RB.velocity.y, 0);


			// jump button
			if (state.Buttons.A == ButtonState.Pressed && prevState.Buttons.A != ButtonState.Pressed) {
				// check if grounded
				if (grounded) {
					RB.velocity = new Vector2 (RB.velocity.x, 0);
					RB.AddForce (new Vector2 (0, jumpHeight));
					grounded = false;
					groundedBuffer = 1;
				} else if (onWallLeft || onWallRight) {
					WallJump ();
				} else if (doubleJump) {
					BreakLine ();
					RB.velocity = new Vector2 (RB.velocity.x, 0);
					RB.AddForce (new Vector2 (0, jumpHeight));
					doubleJump = false;
				}
			}

			if (onWallLeft || onWallRight) {
				RB.velocity = new Vector2 (0, 0);
			}

			// aiming button controls
			if (state.Triggers.Left > .5f) {    //  && Controller){

			}

			// input for shooting primary
			if (state.Triggers.Right > .5f) {

			}
			// input for  swinging
			if (prevState.Triggers.Right <= .5f && state.Triggers.Right > .5f && !onWallLeft && !onWallRight && !grounded) {
				if (facingRight) {
					grappleDirection = new Vector2 (1, 1);
				} else {
					grappleDirection = new Vector2 (-1, 1);
				}

				RaycastHit2D swingHit = Physics2D.Raycast (transform.position, grappleDirection, Mathf.Infinity, groundMask);
				swingPoint = swingHit.point;
				SwingRadius = swingHit.distance;
				grappleAnchor.transform.position = swingPoint;
				grappleAnchor.SetActive (true);
				LR.enabled = true;

				swinging = true;
				leftRightEnabled = false;
			}
			// input for releasing swing
			if (prevState.Triggers.Right > .5f && state.Triggers.Right <= .5f) {
				BreakLine ();
			}
			// inptut for secondary released
			if (state.Buttons.RightShoulder == ButtonState.Released && prevState.Buttons.RightShoulder == ButtonState.Pressed) {

			}
			// input for shooting secondary
			if (state.Buttons.RightShoulder == ButtonState.Pressed) {
			}
			// menu control
			if (state.Buttons.Start == ButtonState.Pressed && prevState.Buttons.Start == ButtonState.Released && Time.timeScale == 1) {
			
				
			}

			// x button for swinging
			if (state.Buttons.X == ButtonState.Pressed && prevState.Buttons.X == ButtonState.Released && dash && canAttack) {
				
				StartCoroutine (CircleAttack ());

				// old call for dash attack
				//StartCoroutine (DashAttack(state.ThumbSticks.Left.X, state.ThumbSticks.Left.Y));

			}
			if (prevState.Buttons.X == ButtonState.Pressed && state.Buttons.X == ButtonState.Released) {
				
			}
		}
			// change up velocity based on swinging state
			if (swinging) {
				CheckLineBreaks ();
				LineGraphicsUpdate ();

				// reel in line
				if (SwingRadius > 1) {
					SwingRadius -= .05f;
				}

				if (Vector2.Distance (transform.position, swingPoint) > SwingRadius) {
					
					transform.position = Vector2.MoveTowards (transform.position, swingPoint, Vector2.Distance (transform.position, swingPoint) - SwingRadius);
					RB.velocity =  (transform.position - prevPosition)/Time.deltaTime;
				}
			}
			
		
		prevPosition = transform.position;
	}

	void WallJump(){
		RB.velocity = new Vector2 (RB.velocity.x, 0);
		dash = true;
		if (onWallRight) {
			RB.AddForce (new Vector2 (-200, jumpHeight));
			onWallRight = false;
		} else {
			RB.AddForce (new Vector2 (200, jumpHeight));
			onWallLeft = false;
		}
		leftRightEnabled = false;
		wallBuffer = 2;
		StartCoroutine (LeftRightEnabler ());
	}

	IEnumerator LeftRightEnabler(){
		yield return new WaitForSeconds (.05f);
		leftRightEnabled = true;
	}
	void LineGraphicsUpdate(){
		LR.SetPosition (0, transform.position);
		LR.SetPosition (1, swingPoint);
		LR.material.mainTextureScale = new Vector2(Vector2.Distance (transform.position, swingPoint),1);
	}

	void BreakLine(){
		grappleAnchor.SetActive (false);
		LR.enabled = false;
		swinging = false;
		leftRightEnabled = true;
	}

	void CheckLineBreaks(){
		Vector2 dir =  new Vector3(swingPoint.x, swingPoint.y, 1) - transform.position;
		RaycastHit2D hit = Physics2D.Raycast (transform.position, dir, Mathf.Infinity, groundMask);
		if (hit.point != swingPoint) {
			swingPoint = hit.point;
			SwingRadius = hit.distance;
			grappleAnchor.transform.position = swingPoint;
		}
			
	}

	IEnumerator CircleAttack(){
		//inputEnabled = false;
		canAttack = false;
		swingEffect.SetActive (true);
		yield return new WaitForSeconds (.05f);

		RaycastHit2D[] hit =  Physics2D.CircleCastAll (transform.position, 1f, Vector2.zero, 0f, playerGroundMask);

		foreach (RaycastHit2D player in hit){
			if (player.collider.gameObject != gameObject) {
				player.collider.GetComponent<HealthScript> ().DealDamage (100);
				SS.IncrementKill (playerNumber);
			}
		}

		yield return new WaitForSeconds (.05f);
		swingEffect.SetActive (false);
		inputEnabled = true;
		yield return new WaitForSeconds (.15f);
		canAttack = true;
	}

	// old dash attack variant
	/*
	IEnumerator DashAttack(float x, float y){

		inputEnabled = false;

		if (!grounded) {
			dash = false;
		}
		BreakLine ();
		if (onWallLeft) {
			onWallLeft = false;
		}
		if (onWallRight){
			onWallRight = false;
		}
		// constrain x
		if (x > .5f) {
			x = 1;
		} else if (x < -.5f) {
			x = -1;
		} else {
			x = 0;
		}

		// constrain y
		if (y > .5f) {
			y = 1;
		} else if (y < -.5f) {
			y = -1;
		} else {
			y = 0;
		}
		// swing effect

		SR.color = Color.red;

		RaycastHit2D hitCheck = Physics2D.Raycast (transform.position, new Vector3(x, y, 0), 4.5f, playerGroundMask);
		RB.gravityScale = 0;
		 if (x == 0 || y == 0) {
			RB.velocity = new Vector3 (x * DashSpeed, y * DashSpeed, 0);
			yield return new WaitForSeconds (.05f);
		} else {
			RB.velocity = new Vector3 (x * DashSpeed/1.5f, y * DashSpeed/1.5f, 0);
			yield return new WaitForSeconds (.05f);

		}

	




		// check hit and do damage
		if (hitCheck.collider != null && hitCheck.collider.tag == "Player"){

			// send message to health script
			Debug.Log ("hit");
			//hitCheck.collider.gameObject.GetComponent<HealthScript>().DealDamage(100);
		}

		RB.gravityScale = 3;

		SR.color = Color.white;
		RB.velocity = Vector3.zero;
		RB.gravityScale = 3;
		inputEnabled = true;

	}
	*/
	public int GetPlayerNumber(){
		return playerNumber;
	}
}
