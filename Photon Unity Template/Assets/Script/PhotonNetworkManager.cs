using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Manages Photon Engine Connection.
/// </summary>
public class PhotonNetworkManager : MonoBehaviourPunCallbacks
{
    // Simplified Singleton : Unity-specified. implementations should be different in the outside of Unity!
    #region Singleton
    public static PhotonNetworkManager Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }
    #endregion

    private Action<string> onInitSuccess;
    private bool isInitialized;

    public void Init(Action<string> onInitSuccess = null)
    {
        if (!isInitialized)
        {
            PhotonNetwork.ConnectUsingSettings(); // Connects to Photon master server
            isInitialized = true;

            this.onInitSuccess = onInitSuccess;
        }
    }

    public override void OnConnectedToMaster()
    {
        // Scene Synchronization causes several side effects, so we will use Photon Game in one scene
        //PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.JoinLobby();
        onInitSuccess(PhotonNetwork.CloudRegion);
    }

    public IEnumerator DeInitEnumerator()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }

        yield return new WaitUntil(() => !PhotonNetwork.InRoom);

        PhotonNetwork.LeaveLobby();

        yield return new WaitUntil(() => !PhotonNetwork.InLobby);

        PhotonNetwork.Disconnect();

        yield return new WaitUntil(() => !PhotonNetwork.IsConnected);
    }
}