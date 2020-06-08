﻿using UnityEngine;

using Photon.Pun;
using Photon.Realtime;

public class PhotonPlayer : MonoBehaviour
{
    public PhotonView view;
    public MeshRenderer cube;
    public TextMesh hpText;

    public int id;
    public int hp;

    private const int HP_MAX = 100;
    private const string HP_STRING = " HP";
    private const string RPC_UPDATE_HP_METHOD_NAME = "RPCUpdateHP";
    private readonly Vector3 STANDBY_POSITION = new Vector3(2000f, 2000f, 0);

    private void Start()
    {
        hp = HP_MAX;

        if (view.IsMine)
        {
            cube.material.color = Color.yellow;
        }
    }

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

    public void HasKilled()
    {
        gameObject.transform.position = STANDBY_POSITION;
    }
}