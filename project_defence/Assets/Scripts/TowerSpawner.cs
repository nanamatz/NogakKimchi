using UnityEngine;
using System.Collections;

public class TowerSpawner : MonoBehaviour
{
	[SerializeField]
	private	TowerTemplate[]		towerTemplate;				// 타워 정보 (공격력, 공격속도 등)
	[SerializeField]
	private	EnemySpawner		enemySpawner;				// 현재 맵에 존재하는 적 리스트 정보를 얻기 위해..
	[SerializeField]
	private	PlayerGold			playerGold;					// 타워 건설 시 골드 감소를 위해..
	[SerializeField]
	private	SystemTextViewer	systemTextViewer;           // 돈 부족, 건설 불가와 같은 시스템 메시지 출력

	[SerializeField]
	public GameObject			buildEffect;                // 설치 할 때 나오는 이펙트
	[SerializeField]
	public GameObject			sellEffect;					// 판매 할 때 나오는 이펙트

	private	bool				isOnTowerButton = false;	// 타워 건설 버튼을 눌렀는지 체크
	private	GameObject			followTowerClone = null;	// 임시 타워 사용 완료 시 삭제를 위해 저장하는 변수
	private	int					towerType;                  // 타워 속성

	private Ray ray;
	private RaycastHit hit;
	public	bool				IsOnTowerButton => isOnTowerButton;


    public void set_lock(GameObject[] _Lock)
    {
        for(int i = 0; i < towerTemplate.Length; i++)
        {
			//Debug.Log(towerTemplate[i].weapon[0].isLock);
			if (!towerTemplate[i].isLock)
            {
				_Lock[i].GetComponent<Lock>().LockOff();
			}
        }
    }

    public void ReadyToSpawnTower(int type)
	{
		int tmp = towerType;
		towerType = type;
		if (!towerTemplate[towerType].isLock)
		{

			// 버튼을 중복해서 누르는 것을 방지하기 위해 필요
			if (isOnTowerButton == true)
			{
				towerType = tmp;
				Debug.Log("return");
				return;
			}

			// 타워 건설 가능 여부 확인
			// 타워를 건설할 만큼 돈이 없으면 타워 건설 X
			if (towerTemplate[towerType].weapon[0].cost > playerGold.CurrentGold)
			{
				towerType = tmp;
				// 골드가 부족해서 타워 건설이 불가능하다고 출력
				systemTextViewer.PrintText(SystemType.Money);
				return;
			}

			// 마우스를 따라다니는 임시 타워 생성
			followTowerClone = Instantiate(towerTemplate[towerType].followTowerPrefab);
			// 타워 건설 버튼을 눌렀다고 설정
			isOnTowerButton = true;
			// 타워 건설을 취소할 수 있는 코루틴 함수 시작
			StartCoroutine("OnTowerCancelSystem");
		}
	}

	public void SpawnTower(Transform tileTransform)
	{
		// 타워 건설 버튼을 눌렀을 때만 타워 건설 가능
		if ( isOnTowerButton == false )
		{
			return;
		}
		
		Tile tile = tileTransform.GetComponent<Tile>();
		// 2. 현재 타일의 위치에 이미 타워가 건설되어 있으면 타워 건설 X
		if ( tile.IsBuildTower == true )
		{
			// 현재 위치에 타워 건설이 불가능하다고 출력
			systemTextViewer.PrintText(SystemType.Build);
			return;
		}
		// 2칸 타워을 건설할 수 없는 곳이면 건설X
		else if (towerTemplate[towerType].weapon[0].tileType == TileType.Two)
        {
			if (Physics.Raycast(tile.transform.position, transform.right, out hit, 1))
			{
				if (hit.transform.tag == "TileRoad")
				{
					// 현재 위치에 타워 건설이 불가능하다고 출력
					systemTextViewer.PrintText(SystemType.Build);
					return;
				}
			}
		}


		// 다시 타워 건설 버튼을 눌러서 타워를 건설하도록 변수 설정
		isOnTowerButton = false;
		// 타워가 건설되어 있음으로 설정
		tile.IsBuildTower = true;
		// 타워 건설에 필요한 골드만큼 감소
		playerGold.CurrentGold -= towerTemplate[towerType].weapon[0].cost;
		Vector3 position;
		// 타워 TileType이 1이면 1칸 사용, 2면 2칸 사용
		if (towerTemplate[towerType].weapon[0].tileType == TileType.One)
        {
			// 1칸인 경우
			// 선택한 타일의 위치에 타워 건설 (타일보다 z축 -1의 위치에 배치, y축 0.5위로 배치)
			position = tileTransform.position + Vector3.back + Vector3.up / 2;
		}
        else
        {
			// 2칸인 경우
			position = tileTransform.position + Vector3.right/2 + Vector3.back + Vector3.up / 2;
		}
		// 타워 실치 이펙트 보이기
		buildEffect.SetActive(true);
		buildEffect.GetComponent<TowerBuildEffect>().Boom();
		buildEffect.transform.position = position + Vector3.down / 2;

		// 타워 설치 사운드 재생
		GetComponent<AudioSource>().Play();
		GameObject clone = Instantiate(towerTemplate[towerType].towerPrefab, position, Quaternion.identity);


		// 타워 무기에 enemySpawner, playerGold, tile 정보 전달
		clone.GetComponent<TowerWeapon>().Setup(this, enemySpawner, playerGold, tile);

		// 새로 배치되는 타워가 버프 타워 주변에 배치될 경우
		// 버프 효과를 받을 수 있도록 모든 버프 타워의 버프 효과 갱신
		OnBuffAllBuffTowers();

		// 타워를 배치했기 때문에 마우스를 따라다니는 임시 타워 삭제
		Destroy(followTowerClone);

		// 타워 건설을 취소할 수 있는 코루틴 함수 중지
		StopCoroutine("OnTowerCancelSystem");
	}

	private IEnumerator OnTowerCancelSystem()
	{
		while ( true )
		{
			// ESC키 또는 마우스 오른쪽 버튼을 눌렀을 때 타워 건설 취소
			if ( Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1) )
			{
				isOnTowerButton = false;
				// 마우스를 따라다니는 임시 타워 삭제
				Destroy(followTowerClone);
				break;
			}

			yield return null;
		}
	}

	public void OnBuffAllBuffTowers()
	{
		GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower");
		
		for ( int i = 0; i < towers.Length; ++ i )
		{
			TowerWeapon weapon = towers[i].GetComponent<TowerWeapon>();

			if ( weapon.WeaponType == WeaponType.Buff )
			{
				weapon.OnBuffAroundTower();
			}
		}
	}

	public void SetTowerLock(GameObject _Lock, int towerType, bool setLock)
	{
		towerTemplate[towerType].isLock = setLock;

		_Lock.GetComponent<Lock>().LockOff();
		if (towerType >= 1)
		{
			_Lock.GetComponent<Lock>().LockOffImage();
		}
	}
}


/*
 * File : TowerSpawner.cs
 * Desc
 *	: 타워 생성 제어
 *	
 * Functions
 *	: ReadyToSpawnTower() - 타워 건설 버튼을 눌렀을 때 마우스를 쫓아다니는 임시 타워 생성
 *	: SpawnTower() - 매개변수의 위치에 타워 생성
 *	: OnTowerCancelSystem() - 타워 건설이 활성화 되었을 때 타워 건설 취소
 *	: OnBuffAllBuffTowers() - 맵에 배치된 모든 버프 타워의 버프 효과 갱신
 *	
 */