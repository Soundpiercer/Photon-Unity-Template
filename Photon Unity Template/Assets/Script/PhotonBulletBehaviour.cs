using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Pun;
using Photon.Realtime;

public class PhotonBulletBehaviour : MonoBehaviour
{
    public PhotonView view;
    public MeshRenderer cube;
    public Vector3 velocity;

        private bool hasCollided;

        private const float LIFESPAN = 8f;
        private const int DAMAGE = 10;

        private const string RPC_MOVE_METHOD_NAME = "RPCMove";
        private const string RPC_DESTROY_METHOD_NAME = "RPCDestroy";

        private void Start()
        {
            cube.material.color = Color.red;
        }

        public void Init(Vector3 velocity)
        {
            this.velocity = velocity;

            StartCoroutine(SelfDestructEnumerator());
            view.RPC(RPC_MOVE_METHOD_NAME, RpcTarget.AllBuffered, 0);
        }

        [PunRPC]
        private void RPCMove(int dummy)
        {
            StartCoroutine(MoveEnumerator());
        }

    // Physical movements
        private IEnumerator MoveEnumerator()
        {
            while (!hasCollided)
            {
                gameObject.transform.position += velocity;
                yield return null;
            }
        }


    // Self-destruct after lifespan
        private IEnumerator SelfDestructEnumerator()
        {
            yield return new WaitForSeconds(LIFESPAN);
            view.RPC(RPC_DESTROY_METHOD_NAME, RpcTarget.AllBuffered, 0);
        }

    // The player hits the enemy on collision.
        private void OnCollisionEnter(Collision collision)
        {
            GameObject target = collision.transform.gameObject;
            PhotonPlayer player = target.GetComponent<PhotonPlayer>();
            if (player != null)
            {
            // enemy is killed when the HP becomes below 0.
                player.hp -= DAMAGE;
                if (player.hp <= 0)
                {
                    player.HasKilled();
                }
            }

            hasCollided = true;

            if (!view.IsMine)
                view.RPC(RPC_DESTROY_METHOD_NAME, RpcTarget.AllBuffered, 0);
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