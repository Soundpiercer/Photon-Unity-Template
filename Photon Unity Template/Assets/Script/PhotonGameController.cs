using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Pun;
using Photon.Realtime;

public class PhotonGameController : MonoBehaviourPunCallbacks
{
    public PhotonChatController chatController;

    [Header("Panels")]
    public GameObject lobbyPanel;
    public GameObject gamePanel;

    [Header("Lobby Panel")]
    public Text photonStatusText;
    public Text photonDetailText;
    public GameObject quickStartButton;
    public GameObject logoutButton;

    private string defaultSuccessMessage = string.Empty;
    private const string QUICKSTART_SUCCESS_MESSAGE = "\nSuccessfully joined by QuickStart, ";
    private const string ONJOINROOMFAILED_MESSAGE = "Failed to join a room";
    private const string ONCREATEROOMFAILED_MESSAGE = "Failed to create room... trying again";
    private const int MAX_CCU_FOR_FREE_TIER = 20;

    private void Start()
    {
        PhotonNetworkManager.Instance.Init(
            onInitSuccess: region =>
            {
                defaultSuccessMessage = "We are now connected to the " + region + " server!";
                photonStatusText.text = defaultSuccessMessage;

                quickStartButton.SetActive(true);
                StartCoroutine(DisplayRoomNumberEnumerator());
            });
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

    public void Logout()
    {
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
    public Transform[] spawnPoints;
    public GameObject photonPlayerPrefab;
    public GameObject photonBulletPrefab;
    private PhotonPlayer myPlayer;
    private readonly Vector3 UP = new Vector3(0, 10);

    private const float DISTANCE_FROM_BODY = 60f;
    private const float BULLET_SPEED = 6f;

    private void GameSetup()
    {
        // UI Setup
        lobbyPanel.SetActive(false);
        gamePanel.SetActive(true);

        // Chat Setup
        chatController.Init();

        StartPhotonGame();
    }

    // Start is called before the first frame update
    private void StartPhotonGame()
    {
        int point = PhotonNetwork.IsMasterClient ? 0 : 1;

        myPlayer = PhotonNetwork.Instantiate(
            photonPlayerPrefab.name,
            spawnPoints[point].position,
            Quaternion.identity,
            0)
            .GetComponent<PhotonPlayer>();
    }

    public void MoveUp()
    {
        myPlayer.transform.localPosition += UP;
    }

    public void MoveDown()
    {
        myPlayer.transform.localPosition -= UP;
    }

    public void Fire()
    {
        Vector3 fireDirection = (PhotonNetwork.IsMasterClient ? Vector3.right : Vector3.left);

        PhotonBulletBehaviour bullet = PhotonNetwork.Instantiate(
            photonBulletPrefab.name,
            myPlayer.transform.position + (fireDirection * DISTANCE_FROM_BODY),
            Quaternion.identity,
            0)
            .GetComponent<PhotonBulletBehaviour>();

        bullet.Init(fireDirection * BULLET_SPEED);
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