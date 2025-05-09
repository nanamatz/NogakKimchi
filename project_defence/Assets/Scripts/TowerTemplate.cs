using UnityEngine;

[CreateAssetMenu]
public class TowerTemplate : ScriptableObject
{
	public GameObject towerPrefab;      // 타워 생성을 위한 프리팹
	public GameObject followTowerPrefab;    // 임시 타워 프리팹
	public Weapon[] weapon;             // 레벨별 타워(무기) 정보
	public int maxTowerLV; // 최대 타워 레벨
	public bool isLock; // 라운드 별 잠금 해제를 위한 Lock

	[System.Serializable]
	public struct Weapon
	{
		public Sprite sprite;   // 보여지는 타워 이미지 (UI)
		public float damage;    // 공격력
		public float slow;  // 감속 퍼센트 (0.2 = 20%)
		public float buff;  // 공격력 증가율 (1.2 = 120%)
		public float rate;  // 공격 속도
		public float range; // 공격 범위
		public int cost;    // 필요 골드 (0레벨 : 건설, 1~레벨 : 업그레이드)
		public int sell;   // 타워 판매 시 획득 골드
		public TileType tileType; // 타일 차지 개수(1, 2)
		public float explosionRange; // 폭발 범위
		public Sprite projectileSprite; // 총알 스프라이트
        public Sprite[] projectileSprites; // 총알 스프라이트
        public Sprite upgradeImage1;	// 업그레이드 버튼1 이미지 
		public Sprite upgradeImage2; // 업그레이드 버튼2 이미지 
	}
}


/*
 * File : TowerTemplate.cs
 * Desc
 *	: ????뺣낫 ???
 * 
 */