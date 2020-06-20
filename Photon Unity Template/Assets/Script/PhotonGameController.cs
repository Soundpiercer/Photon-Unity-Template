using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Photon Game Scene Controller.
/// </summary>
public class PhotonGameController : MonoBehaviourPunCallbacks
{
    public PhotonView view;
    public PhotonChatController chatController;

    [Header("Panels")]
    public GameObject lobbyPanel;
    public GameObject gamePanel;

    [Header("Lobby Panel")]
    public Text photonStatusText;
    public Text photonDetailText;
    public Text fpsText;
    public GameObject quickStartButton;
    public GameObject logoutButton;

    private string defaultSuccessMessage = string.Empty;
    private const string QUICKSTART_SUCCESS_MESSAGE = "\nSuccessfully joined by QuickStart, ";
    private const string ONJOINROOMFAILED_MESSAGE = "Failed to join a room";
    private const string ONCREATEROOMFAILED_MESSAGE = "Failed to create room... trying again";

    private const string RPC_DISPLAY_SYNCHRONIZATION_TIME_METHOD_NAME = "RPCDisplaySynchronizationTime";
    private const string RPC_SET_OCCUPIED_METHOD_NAME = "RPCSetOccupied";
    private const string RPC_SET_UNOCCUPIED_METHOD_NAME = "RPCSetUnOccupied";

    private const int MAX_CCU_FOR_FREE_TIER = 20;

    private void Start()
    {
        Application.targetFrameRate = 60;
        PhotonNetworkManager.Instance.Init(
            onInitSuccess: region =>
            {
                defaultSuccessMessage = "We are now connected to the " + region + " server!";
                photonStatusText.text = defaultSuccessMessage;

                quickStartButton.SetActive(true);
                StartCoroutine(DisplayRoomNumberEnumerator());
            });
    }

    private void Update()
    {
        fpsText.text = "FPS : " + System.Math.Round((1 / Time.deltaTime), 4);

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.X)) Fire();
        if (Input.GetKeyDown(KeyCode.C)) Jump();
        if (Input.GetKeyDown(KeyCode.V)) Duck();
#endif
    }

    private IEnumerator DisplayRoomNumberEnumerator()
    {
        while (true)
        {
            if (PhotonNetwork.InRoom)
            {
                photonStatusText.text = defaultSuccessMessage + QUICKSTART_SUCCESS_MESSAGE + PhotonNetwork.CurrentRoom.Name;
            }
            else
            {
                photonStatusText.text = defaultSuccessMessage;
            }

            yield return new WaitForSeconds(1f);
        }
    }

    #region UI Interactions
    public void QuickStart()
    {
        // If there is no room, OnJoinRandomFailed -> CreateRandomRoom will be called. The player will be Room Host.
        PhotonNetwork.JoinRandomRoom();

        quickStartButton.SetActive(false);
        logoutButton.SetActive(true);
    }

    // @ TODO : EndPhotonGame-Logout 일원화
    // @ TODO : RPC Method 위치 추가로 정리 가능한가?
    public void Logout()
    {
        // notice to all players that my player's spawn point is now available
        view.RPC(RPC_SET_UNOCCUPIED_METHOD_NAME, RpcTarget.AllBuffered, myPlayer.id);

        PhotonNetwork.LeaveRoom();
        photonDetailText.text = string.Empty;

        quickStartButton.SetActive(true);
        logoutButton.SetActive(false);
    }

    public void Exit()
    {
        StartCoroutine(ExitEnumerator());
    }

    private IEnumerator ExitEnumerator()
    {
        yield return StartCoroutine(PhotonNetworkManager.Instance.DeInitEnumerator());
        Application.Quit();
    }
    #endregion

    #region MonoBehaviourPunCallbacks Event Listener
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        photonDetailText.text = ONJOINROOMFAILED_MESSAGE;
        CreateRandomRoom();
    }

    private void CreateRandomRoom() // trying to create our own room
    {
        photonDetailText.text = "Creating room now";
        int r = Random.Range(0, MAX_CCU_FOR_FREE_TIER); // Allows Up to Maximum Users for Free Tier (20)

        RoomOptions option = new RoomOptions()
        {
            IsVisible = true,
            IsOpen = true,
            MaxPlayers = MAX_CCU_FOR_FREE_TIER
        };

        PhotonNetwork.CreateRoom("Room " + r, option);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        photonDetailText.text = ONCREATEROOMFAILED_MESSAGE;
        CreateRandomRoom();
    }

    public override void OnJoinedRoom()
    {
        photonDetailText.text = string.Empty;
        GameSetup();
    }
    #endregion

    #region Photon Game
    [Header("Photon Game")]
    public GameObject photonPlayerPrefab;
    public Transform[] spawnPoints;
    public bool[] isOccupied;

    private PhotonPlayer myPlayer;
    
    [Header("Game Panel")]
    public Text synchronizationTimeText;

    private void GameSetup()
    {
        // UI Setup
        lobbyPanel.SetActive(false);
        gamePanel.SetActive(true);

        // Chat Setup
        chatController.Init();

        // For Synchronization Latency Check
        view.RPC(RPC_DISPLAY_SYNCHRONIZATION_TIME_METHOD_NAME, RpcTarget.AllBuffered, 0);

        // START!
        StartPhotonGame();
    }

    [PunRPC]
    private void RPCDisplaySynchronizationTime(int dummy)
    {
        synchronizationTimeText.text = "Synchronized at : " + System.DateTime.Now.Ticks.ToString();
    }

    private void StartPhotonGame()
    {
        int id = GetVacantSlotID();

        // instantiate the player with position and rotation specified.
        myPlayer = PhotonNetwork.Instantiate(
            photonPlayerPrefab.name,
            spawnPoints[id].position,
            Quaternion.identity,
            0)
            .GetComponent<PhotonPlayer>();
        myPlayer.Init(id);

        // notice to all players that my player's spawn point is now occupied
        view.RPC(RPC_SET_OCCUPIED_METHOD_NAME, RpcTarget.AllBuffered, id);
    }

    [PunRPC]
    private void RPCSetOccupied(int id)
    {
        isOccupied[id] = true;
    }

    [PunRPC]
    private void RPCSetUnOccupied(int id)
    {
        isOccupied[id] = false;
    }

    private int GetVacantSlotID()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (isOccupied[0]) return 1;
            else return 0;
        }
        else
        {
            if (isOccupied[1]) return 0;
            else return 1;
        }
    }

    public void Jump()
    {
        myPlayer.Jump();
    }

    public void Fire()
    {
        myPlayer.Fire();
    }

    public void Duck()
    {
        myPlayer.Duck();
    }

    public void EndPhotonGame()
    {
        // UI DeInit
        gamePanel.SetActive(false);
        lobbyPanel.SetActive(true);

        // Chat DeInit
        chatController.DeInit();

        Logout();
    }
    #endregion
}
