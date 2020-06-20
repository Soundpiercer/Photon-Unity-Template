using System.Collections;
using UnityEngine;

using Photon.Pun;
using Photon.Realtime;

public class PhotonPlayer : MonoBehaviour
{
    public PhotonView view;
    public int id;

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
    private readonly Quaternion QUATERNION_BACKWARDS = Quaternion.Euler(0, 180f, 0);
    private const float ANIMATOR_SPEED = 1.5f;
    private readonly Vector3 STANDBY_POSITION = new Vector3(2000f, 2000f, 0);

    private const float INVINCIBLE_TIME_WHILE_DUCKING = 1.5f / ANIMATOR_SPEED;
    private const float INVINCIBLE_TIME_WHILE_DAMAGED = 3.125f / ANIMATOR_SPEED;

    // Bullet
    private const float BULLET_INIT_DISTANCE_X_FROM_MODEL = 24f;
    private const float BULLET_SPEED = 10f;
    private readonly Vector3 BULLET_INIT_DISTANCE_Y_FROM_GROUND = new Vector3(0, 135f);

    // RPC
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

        audioSource.clip = initVoice;
        audioSource.Play();
    }

    public void Init(int id)
    {
        this.id = id;
        if (id != 0)
        {
            model.transform.rotation = QUATERNION_BACKWARDS;
        }
    }

    // @ TODO : Update 메서드 부하가 센데 빼버릴수 없나?
    private void FixedUpdate()
    {
        // Send and synchronizes my player's status to all players.
        if (view.IsMine)
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

        audioSource.clip = jumpVoice[Random.Range(0, jumpVoice.Length)];
        audioSource.Play();
    }

    [PunRPC]
    private void RPCJump(int dummy)
    {
        animator.SetTrigger("Jump");
    }

    public void Fire()
    {
        view.RPC(RPC_FIRE_METHOD_NAME, RpcTarget.AllBuffered, 0);
    }

    [PunRPC]
    private void RPCFire(int dummy)
    {
        animator.SetTrigger("Fire");

        audioSource.clip = fireVoice;
        audioSource.Play();

        Vector3 fireDirection = id == 0 ? Vector3.right : Vector3.left;

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
        gameObject.transform.position = STANDBY_POSITION;

        audioSource.clip = killedVoice;
        audioSource.Play();
    }
}
