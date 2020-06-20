using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Pun;
using Photon.Realtime;

public class PhotonBulletBehaviour : MonoBehaviour
{
    public PhotonView view;
    public MeshRenderer sphere;
    public Vector3 velocity;

    private const float LIFESPAN = 8f;
    private const int DAMAGE = 10;

    private const string RPC_MOVE_METHOD_NAME = "RPCMove";
    private const string RPC_DESTROY_METHOD_NAME = "RPCDestroy";

    private void Start()
    {
        sphere.material.color = Color.red;
    }

    public void Init(Vector3 velocity)
    {
        this.velocity = velocity;

        StartCoroutine(SelfDestructEnumerator());
        view.RPC(RPC_MOVE_METHOD_NAME, RpcTarget.AllBuffered, 0);
    }

    // Self-destruct after lifespan
    private IEnumerator SelfDestructEnumerator()
    {
        yield return new WaitForSeconds(LIFESPAN);
        view.RPC(RPC_DESTROY_METHOD_NAME, RpcTarget.AllBuffered, 0);
    }

    [PunRPC]
    private void RPCMove(int dummy)
    {
        StartCoroutine(MoveEnumerator());
    }

    // Physical movements
    private IEnumerator MoveEnumerator()
    {
        while (true)
        {
            gameObject.transform.position += velocity;
            yield return new WaitForFixedUpdate();
        }
    }

    // The player hits the enemy on collision.
    private void OnCollisionEnter(Collision collision)
    {
        GameObject target = collision.transform.gameObject;
        PhotonPlayer player = target.GetComponent<PhotonPlayer>();
        if (player != null)
        {
            // enemy is killed when the HP becomes below 0.
            player.GotDamaged(damage: DAMAGE);

            if (player.hp <= 0)
            {
                player.HasKilled();
            }
        }

        if (view.IsMine)
        {
            gameObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        }
        else
        {
            view.RPC(RPC_DESTROY_METHOD_NAME, RpcTarget.AllBuffered, 0);
        }

        TraumaInducer explosion = GameObject.Find("BigExplosion").GetComponent<TraumaInducer>();
        explosion.transform.position = gameObject.transform.position;
        StartCoroutine(explosion.StartEnumerator());
    }

    /// <summary>
    /// Display gameObject Destroy() via Pun RPC.
    /// </summary>
    [PunRPC]
    private void RPCDestroy(int dummy)
    {
        Destroy(gameObject);
    }
}