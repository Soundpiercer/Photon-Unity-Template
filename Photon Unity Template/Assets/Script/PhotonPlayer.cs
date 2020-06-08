using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;

public class PhotonPlayer : MonoBehaviour
{
    private PhotonView view;

    // Start is called before the first frame update
    private void Start()
    {
        view = GetComponent<PhotonView>();
    }
}