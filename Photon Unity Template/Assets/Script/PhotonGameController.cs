﻿using System.Collections;
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
            view.RPC("RPCSetUnOccupied", RpcTarget.AllBuffered, myPlayer.id);
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
        public bool[] isOccupied;
    public GameObject photonPlayerPrefab;
    public GameObject photonBulletPrefab;
    private PhotonPlayer myPlayer;
    private readonly Vector3 UP = new Vector3(0, 10);

        [Header("Game Panel")]
        public Text synchronizationTimeText;

    private const float DISTANCE_FROM_BODY = 60f;
    private const float BULLET_SPEED = 6f;

    private void GameSetup()
    {
        // UI Setup
        lobbyPanel.SetActive(false);
        gamePanel.SetActive(true);

        // Chat Setup
        chatController.Init();

            // For Synchronization Latency Check
            view.RPC("RPCDisplaySynchronizationTime", RpcTarget.AllBuffered, 0);

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
            int id = SelectTransform();

            myPlayer = PhotonNetwork.Instantiate(
                photonPlayerPrefab.name,
                spawnPoints[id].position,
                Quaternion.identity,
                0)
                .GetComponent<PhotonPlayer>();

            view.RPC("RPCSetOccupied", RpcTarget.AllBuffered, id);
            myPlayer.id = id;
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

        private int SelectTransform()
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
            Vector3 fireDirection = (myPlayer.id == 0 ? Vector3.right : Vector3.left);

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