using System.Collections;
using UnityEngine;

using Photon.Pun;
using Photon.Realtime;

public class PhotonPlayer : MonoBehaviour
{
    public PhotonView view;
    public Animator animator;
    public AudioSource audioSource;
    public AudioClip initVoice;
    public AudioClip[] jumpVoice;
    public AudioClip fireVoice;
    public AudioClip[] damageVoice;
    public AudioClip killedVoice;

    public TextMesh hpText;

    public int id;
    public int hp;

    private const int HP_MAX = 100;
    private const string HP_STRING = " HP";

    private const float ANIMATOR_SPEED = 1.5f;
    private const float INVINCIBLE_TIME_WHILE_DAMAGED = 3.125f / ANIMATOR_SPEED;

    private const string RPC_UPDATE_HP_METHOD_NAME = "RPCUpdateHP";

    private readonly Vector3 STANDBY_POSITION = new Vector3(2000f, 2000f, 0);

    private void Start()
    {
        animator.speed = ANIMATOR_SPEED;
        hp = HP_MAX;

        // Changes the HP Text Color to yellow to identify which player is mine.
        if (view.IsMine)
        {
            hpText.color = Color.yellow;
        }

        audioSource.clip = initVoice;
        audioSource.Play();
    }

    // @ TODO : Update 메서드 부하가 센데 빼버릴수 없나?
    private void Update()
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
        animator.SetTrigger("Jump");

        audioSource.clip = jumpVoice[Random.Range(0, jumpVoice.Length)];
        audioSource.Play();
    }

    public void Fire()
    {
        audioSource.clip = fireVoice;
        audioSource.Play();
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
