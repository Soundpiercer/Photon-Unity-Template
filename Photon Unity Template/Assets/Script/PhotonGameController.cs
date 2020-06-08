using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Pun;
using Photon.Realtime;

public class PhotonGameController : MonoBehaviourPunCallbacks
{
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
    /// <summary>
    /// Joins an existing random room
    /// </summary>
    public void QuickStart()
    {
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

    private void CreateRandomRoom()
    {
        photonDetailText.text = "Creating room now";
        int r = Random.Range(0, 20); // allows 20 CCU users per free tier account

        RoomOptions option = new RoomOptions()
        {
            IsVisible = true,
            IsOpen = true,
            MaxPlayers = 20
        };

        PhotonNetwork.CreateRoom("Room " + r, option);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        photonDetailText.text = "Failed to join a room";
        CreateRandomRoom();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        photonDetailText.text = "Failed to create room... trying again";
        CreateRandomRoom();
    }

    public override void OnJoinedRoom()
    {
        photonDetailText.text = string.Empty;
        GameSetup();
    }

    #region Photon Game
    [Header("Photon Game")]
    public Transform[] spawnPoints;
    public GameObject prefab;
    private PhotonPlayer myPlayer;
    private readonly Vector3 LEFT = new Vector3(-10, 0);
    private readonly Vector3 UP = new Vector3(0, 10);
    private readonly Vector3 RIGHT = new Vector3(10, 0);
    private readonly Vector3 DOWN = new Vector3(0, -10);

    private void GameSetup()
    {
        lobbyPanel.SetActive(false);
        gamePanel.SetActive(true);
        StartPhotonGame();
    }

    // Start is called before the first frame update
    private void StartPhotonGame()
    {
        myPlayer = PhotonNetwork.Instantiate(
            prefab.name,
            spawnPoints[Random.Range(0, spawnPoints.Length)].position,
            Quaternion.identity,
            0)
            .GetComponent<PhotonPlayer>();
    }

    public void MoveUp()
    {
        myPlayer.transform.localPosition += UP;
    }

    public void EndPhotonGame()
    {
        gamePanel.SetActive(false);
        lobbyPanel.SetActive(true);

        Logout();
    }
    #endregion
}