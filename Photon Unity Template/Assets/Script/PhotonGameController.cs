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
    public Camera mainCamera;
    public PhotonChatController chatController;

    [Header("Audio")]
    public AudioSource lobbyBGM;
    public AudioSource maingameBGM;

    [Header("Panels")]
    public GameObject lobbyPanel;
    public GameObject gamePanel;

    [Header("Lobby Panel")]
    public Text photonStatusText;
    public Text photonDetailText;
    public Text fpsText;
    public GameObject quickStartButton;
    public GameObject quitButton;
    public GameObject horizontalPlane;
    public LobbyPlayer lobbyPlayer;

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
        lobbyBGM.Play();
        SetLobby3DObjects(true);

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

    private void SetLobby3DObjects(bool isEnter)
    {
        horizontalPlane.SetActive(!isEnter);
        lobbyPlayer.gameObject.SetActive(isEnter);
        if (isEnter)
        {
            StartCoroutine(lobbyPlayer.InitEnumerator());
        }
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

    private void Update()
    {
        // Display FPS.
        fpsText.text = "FPS : " + System.Math.Round((1 / Time.deltaTime), 4);

#if UNITY_EDITOR
        // Enable Key Control
        if (myPlayer != null && Input.GetKeyDown(KeyCode.X)) Fire();
        if (myPlayer != null && Input.GetKeyDown(KeyCode.C)) Jump();
        if (myPlayer != null && Input.GetKeyDown(KeyCode.V)) Duck();

        // LobbyPlayer Raycast Event
        if (Input.GetMouseButtonDown(0))
        {
            StartRaycastEvent(mainCamera.ScreenPointToRay(Input.mousePosition));
        }
#else
        // LobbyPlayer Raycast Event
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            StartRaycastEvent(mainCamera.ScreenPointToRay(Input.GetTouch(0).position));
        }
#endif
    }

    private void StartRaycastEvent(Ray ray)
    {
        RaycastHit hitObject;
        if (Physics.Raycast(ray, out hitObject, Mathf.Infinity))
        {
            if (hitObject.transform.gameObject.GetComponent<LobbyPlayer>() != null)
            {
                lobbyPlayer.PlayAnimationOnRaycastHit();
            }
        }
    }

    #region UI Interactions
    public void QuickStart()
    {
        // If there is no room, OnJoinRandomFailed -> CreateRandomRoom will be called. The player will be Room Host.
        PhotonNetwork.JoinRandomRoom();

        quickStartButton.SetActive(false);
        quitButton.SetActive(false);
    }

    public void Exit()
    {
        StartCoroutine(ExitEnumerator());
    }

    private IEnumerator ExitEnumerator()
    {
        yield return StartCoroutine(PhotonNetworkManager.Instance.DeInitEnumerator());

        lobbyBGM.Stop();
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
        StartPhotonGame();
    }
    #endregion

    #region Photon Game
    [Header("Photon Game")]
    public GameObject photonPlayerPrefab;
    public Transform[] spawnPoints;
    public bool[] isOccupied;

    private PhotonPlayer myPlayer;

    [Header("Game Panel")]
    public GameObject gameDefaultUIRoot;
    public GameObject controlButtonRoot;
    public Text synchronizationTimeText;

    private void StartPhotonGame()
    {
        // Audio
        lobbyBGM.Stop();
        maingameBGM.Play();

        // UI Setup
        lobbyPanel.SetActive(false);
        gamePanel.SetActive(true);
        SetLobby3DObjects(false);

        // For Synchronization Latency Check
        view.RPC(RPC_DISPLAY_SYNCHRONIZATION_TIME_METHOD_NAME, RpcTarget.AllBuffered, 0);

        // START!
        StartCoroutine(StartPhotonGameEnumerator());
    }

    [PunRPC]
    private void RPCDisplaySynchronizationTime(int dummy)
    {
        synchronizationTimeText.text = "Synchronized at : " + System.DateTime.Now.Ticks.ToString();
    }

    private IEnumerator StartPhotonGameEnumerator()
    {
        // Chat Setup
        yield return chatController.InitEnumerator();

        // instantiate and init the player with position and rotation specified.
        int id = GetVacantSlotID();
        myPlayer = PhotonNetwork.Instantiate(
            photonPlayerPrefab.name,
            spawnPoints[id].position,
            Quaternion.identity,
            0)
            .GetComponent<PhotonPlayer>();
        myPlayer.Init(id);

        // notice to all players that my player's spawn point is now occupied
        view.RPC(RPC_SET_OCCUPIED_METHOD_NAME, RpcTarget.AllBuffered, id);

        // Enable User-Controllable UIs
        gameDefaultUIRoot.SetActive(true);
        controlButtonRoot.SetActive(true);
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
        // Don't Jump on damaged, ducking or killed
        if (myPlayer.isInvincible || myPlayer.hasKilled)
            return;

        myPlayer.Jump();
        StartCoroutine(HideButtonsWhileDoingActionEnumerator(PhotonPlayer.JUMP_TIME));
    }

    public void Fire()
    {
        // Don't Fire on damaged, ducking or killed
        if (myPlayer.isInvincible || myPlayer.hasKilled)
            return;

        myPlayer.Fire();
    }

    public void Duck()
    {
        // Don't Duck on damaged, ducking or killed
        if (myPlayer.isInvincible || myPlayer.hasKilled)
            return;

        myPlayer.Duck();
        StartCoroutine(HideButtonsWhileDoingActionEnumerator(PhotonPlayer.INVINCIBLE_TIME_WHILE_DUCKING));
    }

    private IEnumerator HideButtonsWhileDoingActionEnumerator(float time)
    {
        controlButtonRoot.SetActive(false);
        yield return new WaitForSeconds(time);
        controlButtonRoot.SetActive(true);
    }

    public void EndPhotonGame()
    {
        // UI DeInit
        gameDefaultUIRoot.SetActive(false);
        controlButtonRoot.SetActive(false);
        gamePanel.SetActive(false);
        lobbyPanel.SetActive(true);

        // Chat DeInit
        chatController.DeInit();

        // Logout from Photon Room
        Logout();

        // Audio
        maingameBGM.Stop();
        lobbyBGM.Play();

        // UI
        quickStartButton.SetActive(true);
        quitButton.SetActive(true);
        SetLobby3DObjects(true);
    }

    private void Logout()
    {
        // notice to all players that my player's spawn point is now available
        view.RPC(RPC_SET_UNOCCUPIED_METHOD_NAME, RpcTarget.AllBuffered, myPlayer.id);

        PhotonNetwork.LeaveRoom();
        photonDetailText.text = string.Empty;

        // Player DeInit
        myPlayer = null;
    }
    #endregion
}
