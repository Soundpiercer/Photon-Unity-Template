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

    private bool isInitialized;
    private bool hasCollided;

    private const float LIFESPAN = 8f;
    private const int DAMAGE = 10;

    private void Start()
    {
        cube.material.color = Color.red;
    }

    public void Init(Vector3 velocity)
    {
        this.velocity = velocity;
        isInitialized = true;

        StartCoroutine(MoveEnumerator());
        StartCoroutine(SelfDestructEnumerator());
    }

    // Physical movements
    private IEnumerator MoveEnumerator()
    {
        while (!hasCollided)
        {
            if (isInitialized)
            {
                gameObject.transform.position += velocity;
                yield return null;
            }
        }
    }

    // Self-destruct after lifespan
    private IEnumerator SelfDestructEnumerator()
    {
        yield return new WaitForSeconds(LIFESPAN);
        Destroy(gameObject);
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
            else
            {
                player.DisplayHP();
            }
        }

        hasCollided = true;
    }
}