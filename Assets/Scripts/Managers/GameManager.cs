using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;


public class GameManager : MonoBehaviour
{
    public int m_NumRoundsToWin = 5;            
    public float m_StartDelay = 3f;             
    public float m_EndDelay = 3f;               
    public CameraControl m_CameraControl;
    public Text timer;       
    public Text m_MessageText;                  
    public GameObject[] m_TankPrefabs;
    public TankManager[] m_Tanks;               
    public List<Transform> wayPointsForAI;

    private int m_RoundNumber;                  
    private WaitForSeconds m_StartWait;         
    private WaitForSeconds m_EndWait;           
    private TankManager m_RoundWinner;          
    private TankManager m_GameWinner;           

    public bool timeout = false;
    public bool ready = false;
    private int time_left;

    private int roundsToWinInt;
    private int timePerRoundInt;

    public GameObject startButton;
    public GameObject roundsToWinObj;
    public GameObject timePerRoundObj;
    public GameObject roundsToWinText;
    public GameObject timePerRoundText;
    public Text roundsToWin;
    public Text timePerRound;
    

    private void Start()
    {

        m_StartWait = new WaitForSeconds(m_StartDelay);
        m_EndWait = new WaitForSeconds(m_EndDelay);
    }


    private void SpawnAllTanks()
    {
        m_Tanks[0].m_Instance =
            Instantiate(m_TankPrefabs[0], m_Tanks[0].m_SpawnPoint.position, m_Tanks[0].m_SpawnPoint.rotation) as GameObject;
        m_Tanks[0].m_PlayerNumber = 1;
        m_Tanks[0].SetupPlayerTank();

        for (int i = 1; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].m_Instance =
                Instantiate(m_TankPrefabs[i], m_Tanks[i].m_SpawnPoint.position, m_Tanks[i].m_SpawnPoint.rotation) as GameObject;
            m_Tanks[i].m_PlayerNumber = i + 1;
            m_Tanks[i].SetupAI(wayPointsForAI);
        }
    }


    private void SetCameraTargets()
    {
        Transform[] targets = new Transform[m_Tanks.Length];

        for (int i = 0; i < targets.Length; i++)
            targets[i] = m_Tanks[i].m_Instance.transform;

        m_CameraControl.m_Targets = targets;
    }


    private IEnumerator GameLoop()
    {
        yield return StartCoroutine(RoundStarting());
        StartCoroutine(TimeOut());
        yield return StartCoroutine(RoundPlaying());
        yield return StartCoroutine(RoundEnding());

        if (m_GameWinner != null) SceneManager.LoadScene(0);
        else StartCoroutine(GameLoop());
    }

    private IEnumerator TimeOut()
    {
        time_left = timePerRoundInt;
        while (time_left != -1 && !OneTankLeft()){
            if (time_left >= 10 )
            {
                timer.text = "0:" + time_left.ToString();
            }else
            {
                timer.text = "0:0" + time_left.ToString();
            }
            // StartCoroutine(Seconds());
            time_left-=1;
            yield return new WaitForSeconds(1);
        }
        timeout = true;
        yield return null;
    }

    // private IEnumerator Seconds()
    // {   
    //     time_left-=1;
    //     yield return null;
    // }

    public void onClicked(){

        SpawnAllTanks();
        SetCameraTargets();
        StartCoroutine(MenuSettings());

        StartCoroutine(GameLoop());
    }
    
    private IEnumerator MenuSettings()
    {   
        roundsToWinInt = Convert.ToInt32(roundsToWin.text.ToString());
        timePerRoundInt = Convert.ToInt32(timePerRound.text.ToString());
        
        if (roundsToWinInt > 10){
            roundsToWinInt = 10;
        } else if (roundsToWinInt < 1){
            roundsToWinInt = 1;
        }

        m_NumRoundsToWin = roundsToWinInt;

        if (timePerRoundInt >= 60){
            timePerRoundInt = 59;
        }
        else if(timePerRoundInt < 10){
            timePerRoundInt = 10;
        }

        time_left = timePerRoundInt;

        roundsToWinObj.SetActive(false);
        timePerRoundObj.SetActive(false);
        startButton.SetActive(false);
        roundsToWinText.SetActive(false);
        timePerRoundText.SetActive(false);

        yield return null;

    }


    private IEnumerator RoundStarting()
    {
        timeout = false;
        timer.text = timePerRoundInt.ToString();
        ResetAllTanks();
        DisableTankControl();

        m_CameraControl.SetStartPositionAndSize();

        m_RoundNumber++;
        m_MessageText.text = $"ROUND {m_RoundNumber}";

        yield return m_StartWait;
    }


    private IEnumerator RoundPlaying()
    {
        EnableTankControl();

        m_MessageText.text = string.Empty;

        while (!OneTankLeft() && !timeout) yield return null;
    }


    private IEnumerator RoundEnding()
    {
        DisableTankControl();

        m_RoundWinner = null;

        m_RoundWinner = GetRoundWinner();
        if (m_RoundWinner != null) m_RoundWinner.m_Wins++;

        m_GameWinner = GetGameWinner();

        string message = EndMessage();
        m_MessageText.text = message;
        timeout = false;

        yield return m_EndWait;
    }


    private bool OneTankLeft()
    {
        int numTanksLeft = 0;

        for (int i = 0; i < m_Tanks.Length; i++)
        {
            if (m_Tanks[i].m_Instance.activeSelf) numTanksLeft++;
        }

        return numTanksLeft <= 1;
    }

    private TankManager GetRoundWinner()
    {
        if (timeout)
        {
            return null;
        }

        for (int i = 0; i < m_Tanks.Length; i++)
        {
            if (m_Tanks[i].m_Instance.activeSelf)
                return m_Tanks[i];
        }

        return null;
    }

    private TankManager GetGameWinner()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            if (m_Tanks[i].m_Wins == m_NumRoundsToWin)
                return m_Tanks[i];
        }

        return null;
    }


    private string EndMessage()
    {
        var sb = new StringBuilder();

        if (m_RoundWinner != null) sb.Append($"{m_RoundWinner.m_ColoredPlayerText} WINS THE ROUND!");
        else sb.Append("DRAW!");

        sb.Append("\n\n\n\n");

        for (int i = 0; i < m_Tanks.Length; i++)
        {
            sb.AppendLine($"{m_Tanks[i].m_ColoredPlayerText}: {m_Tanks[i].m_Wins} WINS");
        }

        if (m_GameWinner != null)
            sb.Append($"{m_GameWinner.m_ColoredPlayerText} WINS THE GAME!");

        return sb.ToString();
    }


    private void ResetAllTanks()
    {
        for (int i = 0; i < m_Tanks.Length; i++) m_Tanks[i].Reset();
    }


    private void EnableTankControl()
    {
        for (int i = 0; i < m_Tanks.Length; i++) m_Tanks[i].EnableControl();
    }


    private void DisableTankControl()
    {
        for (int i = 0; i < m_Tanks.Length; i++) m_Tanks[i].DisableControl();
    }
}