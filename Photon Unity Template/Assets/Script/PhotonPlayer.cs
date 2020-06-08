using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Pun;
using Photon.Realtime;

public class PhotonPlayer : MonoBehaviour
{
    public PhotonView view;
    public MeshRenderer cube;
    public TextMesh hpText;

    public int hp;
    private const int HP_MAX = 100;
    private readonly Vector3 STANDBY_POSITION = new Vector3(2000f, 2000f, 0);

    private void Start()
    {
        hp = HP_MAX;

        if (view.IsMine)
        {
            cube.material.color = Color.yellow;
        }

        DisplayHP();
    }

    public void DisplayHP()
    {
        hpText.text = hp + " HP";
    }

    public void HasKilled()
    {
        gameObject.transform.position = STANDBY_POSITION;
    }
}