using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhotonGameManager : MonoBehaviour
{
    #region Singleton
    public static PhotonGameManager Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }
    #endregion

    public GameObject explosionPrefab;

    public void PlayExplosion(Vector3 position)
    {
        AudioSource sound = explosionPrefab.GetComponent<AudioSource>();
        ParticleSystem particle = explosionPrefab.GetComponent<ParticleSystem>();
        sound.Stop();
        particle.Clear(true);

        explosionPrefab.transform.position = position;

        sound.Play();
        TraumaInducer explosion = explosionPrefab.GetComponent<TraumaInducer>();
        explosion.PlayExplosion();
    }
}
