using System.Collections;
using UnityEngine;

using Photon.Pun;
using Photon.Realtime;

public class PhotonPlayer : MonoBehaviour
{
    public PhotonView view;
    public int id;
    public bool isInvincible { get { return GetComponent<Collider>().enabled == false; } }
    public bool hasKilled;

    [Header("HP")]
    public int hp;
    public TextMesh hpText;

    [Header("Unity-chan! Model")]
    public GameObject model;
    public Animator animator;

    [Header("Bullet")]
    public GameObject photonBulletPrefab;

    [Header("Audio Source")]
    public AudioSource audioSource;
    public AudioClip initVoice;
    public AudioClip jumpVoice;
    public AudioClip fireVoice;
    public AudioClip[] damageVoice;
    public AudioClip killedVoice;

    #region CONSTANT
    // HP
    private const int HP_MAX = 100;
    private const string HP_STRING = " HP";

    // Model
    public const float ANIMATOR_SPEED = 1.5f;
    private readonly Quaternion QUATERNION_BACKWARDS = Quaternion.Euler(0, -90f, 0);
    private readonly Vector3 STANDBY_POSITION = new Vector3(2000f, 2000f, 0);

    private const float JUMP_ANIMATION_TIME = 1.833f;
    private const float JUMP_STATE_SPEED = 1f;
    private const float DUCK_ANIMATION_TIME = 1.367f;
    private const float DUCK_STATE_SPEED = 1f;
    private const float DAMAGE01_ANIMATION_TIME = 3.567f;
    private const float DAMAGE01_STATE_SPEED = 1f;
    private const float DAMAGE01_INVINCIBLE_MARGIN = -0.4f;

    public const float JUMP_TIME = JUMP_ANIMATION_TIME / JUMP_STATE_SPEED / ANIMATOR_SPEED;
    public const float INVINCIBLE_TIME_WHILE_DUCKING = DUCK_ANIMATION_TIME / DUCK_STATE_SPEED / ANIMATOR_SPEED;
    public const float INVINCIBLE_TIME_WHILE_DAMAGED = DAMAGE01_ANIMATION_TIME / DAMAGE01_STATE_SPEED / ANIMATOR_SPEED + DAMAGE01_INVINCIBLE_MARGIN;

    public const float FIRE_COOLTIME = 0.5f;

    // Bullet
    private const float BULLET_INIT_DISTANCE_X_FROM_MODEL = 24f;
    private const float BULLET_SPEED = 12f;
    private readonly Vector3 BULLET_INIT_DISTANCE_Y_FROM_GROUND = new Vector3(0, 128f);

    // RPC
    private const string RPC_SET_PLAYER_LOOK_BACKWARDS_METHOD_NAME = "RPCSetPlayerLookBackwards";
    private const string RPC_INIT_PLAYER_MODEL_METHOD_NAME = "RPCInitPlayerModel";
    private const string RPC_UPDATE_HP_METHOD_NAME = "RPCUpdateHP";
    private const string RPC_JUMP_METHOD_NAME = "RPCJump";
    private const string RPC_FIRE_METHOD_NAME = "RPCFire";
    private const string RPC_DUCK_METHOD_NAME = "RPCDuck";
    private const string RPC_CHARGE_START_METHOD_NAME = "RPCChargeStart";
    private const string RPC_CHARGE_END_METHOD_NAME = "RPCChargeEnd";
    #endregion

    private void Start()
    {
        hp = HP_MAX;
        if (view.IsMine) // Changes the HP Text Color to yellow to identify which player is mine.
        {
            hpText.color = Color.yellow;
        }

        animator.speed = ANIMATOR_SPEED;
    }

    public void Init(int id)
    {
        this.id = id;
        view.RPC(RPC_SET_PLAYER_LOOK_BACKWARDS_METHOD_NAME, RpcTarget.AllBuffered, id);
        view.RPC(RPC_INIT_PLAYER_MODEL_METHOD_NAME, RpcTarget.All, 0);
    }

    [PunRPC]
    private void RPCSetPlayerLookBackwards(int initializedPlayerId)
    {
        if (initializedPlayerId != 0)
        {
            model.transform.rotation = QUATERNION_BACKWARDS;
        }
    }

    [PunRPC]
    private void RPCInitPlayerModel(int dummy)
    {
        animator.SetTrigger("Jump");
        audioSource.clip = initVoice;
        audioSource.Play();
    }

    private void Update()
    {
        // Send and synchronizes my player's status to all players.
        if (PhotonNetwork.InRoom && view.IsMine)
            view.RPC(RPC_UPDATE_HP_METHOD_NAME, RpcTarget.AllBuffered, hp);
    }

    [PunRPC]
    private void RPCUpdateHP(int value)
    {
        hpText.text = value + HP_STRING;
    }

    public void Jump()
    {
        view.RPC(RPC_JUMP_METHOD_NAME, RpcTarget.AllBuffered, 0);

        audioSource.clip = jumpVoice;
        audioSource.Play();
    }

    [PunRPC]
    private void RPCJump(int dummy)
    {
        animator.SetTrigger("Jump");
        StartCoroutine(PhysicalJumpEnumerator());
        StartCoroutine(MakePlayerInvincibleWhilePlayingAnimationEnumerator(JUMP_TIME));
    }

    #region Physical Jump Implementation
    // if you want to adjust physical movements, change 4 values : premargin, postmargin, V0, T0
    private const float JUMP_PHYSICS_PREMARGIN = 0.25f;
    private const float JUMP_PHYSICS_POSTMARGIN = 0.45f;
    private const int FRAMES_IN_JUMPING = (int)((JUMP_TIME - JUMP_PHYSICS_PREMARGIN - JUMP_PHYSICS_POSTMARGIN) * 60); // FixedUpdate : 60fps

    private const float V0 = 12f;
    private const int T0 = FRAMES_IN_JUMPING / 2;

    private IEnumerator PhysicalJumpEnumerator()
    {
        yield return new WaitForSeconds(JUMP_PHYSICS_PREMARGIN);

        float y0 = transform.position.y; // initYpos
        int frame = 0;
        Vector3 velocity = new Vector3();

        while (frame < FRAMES_IN_JUMPING)
        {
            velocity.y = -(V0 / T0) * (frame - T0); // v = -(v0/t0)(x - t0)
            transform.position += velocity;
            frame++;
            yield return new WaitForFixedUpdate();
        }
        //Debug.LogWarning(frame);
        transform.position = new Vector3(transform.position.x, y0, transform.position.z);
    }
    #endregion

    public void Fire()
    {
        view.RPC(RPC_FIRE_METHOD_NAME, RpcTarget.AllBuffered, id);
    }

    [PunRPC]
    private void RPCFire(int attackerId)
    {
        animator.SetTrigger("Fire");

        audioSource.clip = fireVoice;
        audioSource.Play();

        Vector3 fireDirection = attackerId == 0 ? Vector3.right : Vector3.left;

        PhotonBulletBehaviour bullet = Instantiate(
            photonBulletPrefab,
            transform.position + (fireDirection * BULLET_INIT_DISTANCE_X_FROM_MODEL) + BULLET_INIT_DISTANCE_Y_FROM_GROUND,
            Quaternion.identity)
            .GetComponent<PhotonBulletBehaviour>();

        bullet.Init(fireDirection * BULLET_SPEED);
    }

    public void Duck()
    {
        view.RPC(RPC_DUCK_METHOD_NAME, RpcTarget.AllBuffered, 0);
    }

    [PunRPC]
    private void RPCDuck(int dummy)
    {
        animator.SetTrigger("Duck");

        audioSource.clip = jumpVoice;
        audioSource.Play();

        StartCoroutine(MakePlayerInvincibleWhilePlayingAnimationEnumerator(INVINCIBLE_TIME_WHILE_DUCKING));
    }

    public void ChargeStart()
    {
        view.RPC(RPC_CHARGE_START_METHOD_NAME, RpcTarget.AllBuffered, 0);
    }

    [PunRPC]
    private void RPCChargeStart(int dummy)
    {
        animator.SetTrigger("ChargeStart");
    }

    public void ChargeEnd()
    {
        view.RPC(RPC_CHARGE_END_METHOD_NAME, RpcTarget.AllBuffered, 0);
    }

    [PunRPC]
    private void RPCChargeEnd(int dummy)
    {
        animator.SetTrigger("ChargeEnd");
    }

    public void GotDamaged(int damage)
    {
        hp -= damage;

        animator.SetTrigger("Damaged");

        audioSource.clip = damageVoice[Random.Range(0, damageVoice.Length)];
        audioSource.Play();

        StartCoroutine(MakePlayerInvincibleWhilePlayingAnimationEnumerator(INVINCIBLE_TIME_WHILE_DAMAGED));
    }

    private IEnumerator MakePlayerInvincibleWhilePlayingAnimationEnumerator(float time)
    {
        GetComponent<Collider>().enabled = false;
        yield return new WaitForSeconds(time);
        GetComponent<Collider>().enabled = true;
    }

    public void HasKilled()
    {
        hasKilled = true;
        gameObject.transform.position = STANDBY_POSITION;

        audioSource.clip = killedVoice;
        audioSource.Play();
    }
}
