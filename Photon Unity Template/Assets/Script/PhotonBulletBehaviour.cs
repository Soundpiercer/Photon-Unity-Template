using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Pun;
using Photon.Realtime;

public class PhotonBulletBehaviour : MonoBehaviour
{
    public MeshRenderer sphere;
    public Vector3 velocity;
    public GameObject explosionPrefab;

    private const float LIFESPAN = 8f;
    private const int DAMAGE = 10;

    private void Start()
    {
        sphere.material.color = Color.red;
    }

    public void Init(Vector3 velocity)
    {
        this.velocity = velocity;
        StartCoroutine(SelfDestructEnumerator());
    }

    // Self-destruct after lifespan
    private IEnumerator SelfDestructEnumerator()
    {
        yield return new WaitForSeconds(LIFESPAN);
    }

    // Physical movements should be called by FixedUpdate
    private void FixedUpdate()
    {
        gameObject.transform.position += velocity;
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

            TraumaInducer explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity).GetComponent<TraumaInducer>();
            explosion.PlayExplosion();

            if (player.hp <= 0)
            {
                player.HasKilled();
            }
        }

        Destroy(gameObject);
    }
}