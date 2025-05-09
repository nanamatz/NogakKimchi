using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WaveSystem : MonoBehaviour
{
    [SerializeField]
    public GameObject[] enemyPrefabs;
    [SerializeField]
    private WaveEnemy[] waves;                  // 현재 스테이지의 모든 웨이브 정보
    [SerializeField]
    private TowerSpawner towerSpawner;
    [SerializeField]
    private EnemySpawner enemySpawner;
    private int currentWaveIndex = -1;  // 현재 웨이브 인덱스
    [SerializeField]
    private GameObject[] GameSpeedImage; // 배속
    bool isfause = false;
    float gameSpeed = 1;

    bool isWaveEnd = true;

    [SerializeField]
    public GameObject[] _Lock;

    static public int spawnEnemyCount; // 스폰한 몬스터 숫자


    // 웨이브 정보 출력을 위한 Get 프로퍼티 (현재 웨이브, 총 웨이브)
    public int CurrentWave => currentWaveIndex + 1;     // 시작이 0이기 때문에 +1
    public int MaxWave => waves.Length;

    private void Start()
    {

        // 시작 시 열릴 타워 설정, restart해도 타워들은 열려있음.
        towerSpawner.SetTowerLock(_Lock[0], 0, false);
        towerSpawner.set_lock(_Lock);
    }

    public void cheat()
    {
        for (int i = 0; i < _Lock.Length; i++)
        {
            towerSpawner.SetTowerLock(_Lock[i], i, false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StopCoroutine("WaveSpawnEnemy");
        }
    }

    public void StartWave()
    {
        // 현재 맵에 적이 없고, Wave가 남아있으면
        //if (enemySpawner.EnemyList.Count == 0 && currentWaveIndex < waves.Length - 1 && isWaveEnd)
        if (isWaveEnd)
        {
            // 인덱스의 시작이 -1이기 때문에 웨이브 인덱스 증가를 제일 먼저 함
            currentWaveIndex++;

            // EnemySpawner의 StartWave() 함수 호출. 현재 웨이브 정보 제공
            StartCoroutine("WaveSpawnEnemy");
            // wave 진행 후 열릴 타워 설정
        }
    }



    private IEnumerator WaveSpawnEnemy()
    {
        isWaveEnd = false;
        int wave_enemy_amount = 0;
        for (int i = 0; i < waves[currentWaveIndex].wave.Length; i++)
        {
            wave_enemy_amount += waves[currentWaveIndex].wave[i].maxEnemyCount;
        }
        waves[currentWaveIndex].maxEnemyCount = wave_enemy_amount;
        spawnEnemyCount = 0;
        enemySpawner.StartWave(waves[currentWaveIndex], this);
        // 현재 wave에서 소환이 다 끝날 때까지 기다리기
        while (waves[currentWaveIndex].maxEnemyCount > spawnEnemyCount)
        {
            yield return new WaitForSeconds(0.5f);
        }
        isWaveEnd = true;
        Debug.Log("end");
    }

    //wave별 타워 잠금해제 (currentWaveIndex가 끝나면 타워가 열림)
    public void waveEndTowerLockOff()
    {
        if (currentWaveIndex == 3)
        {
            towerSpawner.SetTowerLock(_Lock[1], 1, false);
        }
        else if (currentWaveIndex == 5)
        {
            towerSpawner.SetTowerLock(_Lock[2], 2, false);
        }
        else if (currentWaveIndex == 10)
        {
            towerSpawner.SetTowerLock(_Lock[3], 3, false);
        }
        else if (currentWaveIndex == 13)
        {
            towerSpawner.SetTowerLock(_Lock[4], 4, false);
        }
        else if (currentWaveIndex == 15)
        {
            towerSpawner.SetTowerLock(_Lock[5], 5, false);
        }
        else if (currentWaveIndex == 18)
        {
            towerSpawner.SetTowerLock(_Lock[6], 6, false);
        }
        else if (currentWaveIndex == 20)
        {
            towerSpawner.SetTowerLock(_Lock[7], 7, false);
        }
    }


    public void SpeedChange()
    {

        if (!isfause)
        {
            if (Time.timeScale < 2)
            {
                Time.timeScale *= 2f;
            }
            else if (Time.timeScale >= 2)
            {
                Time.timeScale = 0.5f;
            }
            gameSpeed = Time.timeScale;


            GameSpeedImage[0].SetActive(false);
            GameSpeedImage[1].SetActive(false);
            GameSpeedImage[2].SetActive(false);
            switch (gameSpeed)
            {
                case 0.5f:
                    GameSpeedImage[0].SetActive(true);
                    break;
                case 1:
                    GameSpeedImage[1].SetActive(true);
                    break;
                case 2:
                    GameSpeedImage[2].SetActive(true);
                    break;
                default:
                    Debug.Log("speedSetError");
                    break;
            }
        }
    }

    public void GamePause()
    {
        if (!isfause)
        {
            Time.timeScale = 0f;
            isfause = true;
        }
        else
        {
            Time.timeScale = gameSpeed;
            isfause = false;
        }
    }
}

[System.Serializable]
public struct WaveEnemy
{
    public Wave[] wave;
    public int maxEnemyCount; // 현재 웨이브 적 등장 숫자
}

[System.Serializable]
public struct Wave
{
    public float spawnTime;     // 현재 웨이브 적 생성 주기
    public int maxEnemyCount; // 현재 웨이브 적 등장 숫자
    public int[] enemyPrefabs;  // 현재 웨이브 적 등장 종류
}


/*
 * File : WaveSystem.cs
 * Desc
 *	: 웨이브 시스템 정보
 *
 */