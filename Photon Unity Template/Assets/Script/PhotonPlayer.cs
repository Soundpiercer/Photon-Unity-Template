using System.Collections;
using UnityEngine;

using Photon.Pun;
using Photon.Realtime;

public class PhotonPlayer : MonoBehaviour
{
    public PhotonView view;
    public int id;
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
    public AudioClip[] jumpVoice;
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

    public const float JUMP_TIME = JUMP_ANIMATION_TIME / JUMP_STATE_SPEED / ANIMATOR_SPEED;
    public const float INVINCIBLE_TIME_WHILE_DUCKING = DUCK_ANIMATION_TIME / DUCK_STATE_SPEED / ANIMATOR_SPEED;
    public const float INVINCIBLE_TIME_WHILE_DAMAGED = DAMAGE01_ANIMATION_TIME / DAMAGE01_STATE_SPEED / ANIMATOR_SPEED;

    // Bullet
    private const float BULLET_INIT_DISTANCE_X_FROM_MODEL = 24f;
    private const float BULLET_SPEED = 12f;
    private readonly Vector3 BULLET_INIT_DISTANCE_Y_FROM_GROUND = new Vector3(0, 128f);

    // RPC
    private const string RPC_SET_PLAYER_ROTATION_METHOD_NAME = "RPCSetPlayerRotation";
    private const string RPC_UPDATE_HP_METHOD_NAME = "RPCUpdateHP";
    private const string RPC_JUMP_METHOD_NAME = "RPCJump";
    private const string RPC_FIRE_METHOD_NAME = "RPCFire";
    private const string RPC_DUCK_METHOD_NAME = "RPCDuck";
    #endregion

    private void Start()
    {
        hp = HP_MAX;
        if (view.IsMine) // Changes the HP Text Color to yellow to identify which player is mine.
        {
            hpText.color = Color.yellow;
        }

        animator.speed = ANIMATOR_SPEED;
        animator.SetBool("isMine", view.IsMine);
        animator.SetTrigger("Entry");
    }

    public void Init(int id)
    {
        this.id = id;
        view.RPC(RPC_SET_PLAYER_ROTATION_METHOD_NAME, RpcTarget.AllBuffered, id);
    }

    [PunRPC]
    private void RPCSetPlayerRotation(int id)
    {
        if (id != 0)
        {
            model.transform.rotation = QUATERNION_BACKWARDS;
        }

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
        // Don't Jump on damaged or ducking
        bool isInvincible = GetComponent<Collider>().enabled == false;
        if (isInvincible)
            return;

        view.RPC(RPC_JUMP_METHOD_NAME, RpcTarget.AllBuffered, 0);

        audioSource.clip = jumpVoice[Random.Range(0, jumpVoice.Length)];
        audioSource.Play();
    }

    [PunRPC]
    private void RPCJump(int dummy)
    {
        animator.SetTrigger("Jump");
        StartCoroutine(PhysicalJumpEnumerator());
    }

    private const float V = 9f;
    private const float G = -0.3266666f * 1.8333f / ANIMATOR_SPEED;

    private IEnumerator PhysicalJumpEnumerator()
    {
        float init = transform.position.y;
        int frame = 0;

        while (transform.position.y >= init)
        {
            transform.position += new Vector3(0, V + frame * G);
            frame++;
            yield return new WaitForFixedUpdate();
        }
        transform.position = new Vector3(transform.position.x, init, transform.position.z);
    }

    public void Fire()
    {
        // Don't Fire if already has killed
        if (hasKilled)
            return;

        // Don't Fire on damaged or ducking
        bool isInvincible = GetComponent<Collider>().enabled == false;
        if (isInvincible)
            return;

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
        // Don't Duck on damaged or ducking
        bool isInvincible = GetComponent<Collider>().enabled == false;
        if (isInvincible)
            return;

        view.RPC(RPC_DUCK_METHOD_NAME, RpcTarget.AllBuffered, 0);
    }

    [PunRPC]
    private void RPCDuck(int dummy)
    {
        animator.SetTrigger("Duck");

        audioSource.clip = jumpVoice[Random.Range(0, jumpVoice.Length)];
        audioSource.Play();

        StartCoroutine(MakePlayerInvincibleWhilePlayingAnimationEnumerator(INVINCIBLE_TIME_WHILE_DUCKING));
    }

    public void GotDamaged(int damage)
    {
        hp -= damage;

        animator.SetTrigger("Damaged");

        audioSource.clip = damageVoice[Random.Range(0, jumpVoice.Length)];
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
