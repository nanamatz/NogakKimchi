using UnityEngine;
using System.Collections;
public enum WeaponType { Gun = 0, Laser, Slow, Buff, Mortar, Shotgun, Spear, Explosion, Strawberry }
public enum WeaponState
{
    SearchTarget = 0, TryAttackGun, TryAttackLaser, TryAttackMortar,
    TryAttackShotgun, TryAttackSpaer, TryAttackExplosion, TryAttackStrawberry
}
public enum TileType { One, Two };

public class TowerWeapon : MonoBehaviour
{
    [Header("Commons")]
    [SerializeField]
    private TowerTemplate towerTemplate;                            // 타워 정보 (공격력, 공격속도 등)
    [SerializeField]
    private Transform spawnPoint;                               // 발사체 생성 위치
    [SerializeField]
    private WeaponType weaponType;                              // 무기 속성 설정

    [SerializeField]
    private TileType tileType;
    public TowerWeapon buffTower;

    [Header("Gun")]
    [SerializeField]
    private GameObject projectilePrefab;                        // 발사체 프리팹

    [Header("Laser")]
    [SerializeField]
    private LineRenderer lineRenderer;                          // 레이저로 사용되는 선(LineRenderer)
    [SerializeField]
    private Transform hitEffect;                                // 타격 효과
    [SerializeField]
    private LayerMask targetLayer;                          // 광선에 부딪히는 레이어 설정

    [Header("LevelUPImage")]
    [SerializeField]
    private GameObject[] LevelUp;

    private int level = 0;                              // 타워 레벨
    private WeaponState weaponState = WeaponState.SearchTarget; // 타워 무기의 상태
    private Transform attackTarget = null;                    // 공격 대상
    private SpriteRenderer spriteRenderer;                          // 타워 오브젝트 이미지 변경용
    private TowerSpawner towerSpawner;
    private EnemySpawner enemySpawner;                          // 게임에 존재하는 적 정보 획득용
    private PlayerGold playerGold;                              // 플레이어의 골드 정보 획득 및 설정
    private Tile ownerTile;                             // 현재 타워가 배치되어 있는 타일

    private float addedDamage;                          // 버프에 의해 추가된 데미지
    private int buffLevel;                              // 버프를 받는지 여부 설정 (0 : 버프X, 1~3 : 받는 버프 레벨)

    // 타워 설치 오디오 클립
    public AudioClip buildClip;
    // 타워 업그레이드 오디오 클립
    public AudioClip upgradeClip;
    // 타워 판매 오디오 클립
    public AudioClip sellClip;

    public Sprite TowerSprite => towerTemplate.weapon[level].sprite;
    public Sprite ProjectileSprite => towerTemplate.weapon[level].projectileSprite;
    public float Damage => towerTemplate.weapon[level].damage;
    public float Rate => towerTemplate.weapon[level].rate;
    public float Range => towerTemplate.weapon[level].range;

    public float ExplosionRange => towerTemplate.weapon[level].explosionRange;
    public int UpgradeCost => Level < MaxLevel ? towerTemplate.weapon[level + 1].cost : 0;
    public int UpgradeCost2 => Level < MaxLevel ? towerTemplate.weapon[level + 2].cost : 0;
    public int SellCost => towerTemplate.weapon[level].sell;
    public int Level => level + 1;
    public int MaxLevel => towerTemplate.maxTowerLV;
    public float Slow => towerTemplate.weapon[level].slow;
    public float Buff => towerTemplate.weapon[level].buff;
    public WeaponType WeaponType => weaponType;
    public TileType TileType => tileType;
    public Sprite UpgradeImage1 => towerTemplate.weapon[level].upgradeImage1;
    public Sprite UpgradeImage2 => towerTemplate.weapon[level].upgradeImage2;

    public float AddedDamage
    {
        set => addedDamage = Mathf.Max(0, value);
        get => addedDamage;
    }
    public int BuffLevel
    {
        set => buffLevel = Mathf.Max(0, value);
        get => buffLevel;
    }

    public void Setup(TowerSpawner towerSpawner, EnemySpawner enemySpawner, PlayerGold playerGold, Tile ownerTile)
    {
        // 타워 설치 사운드 재생
        SoundManager.instance.SFXPlay("TowerSetUp", buildClip);
        spriteRenderer = GetComponent<SpriteRenderer>();
        this.towerSpawner = towerSpawner;
        this.enemySpawner = enemySpawner;
        this.playerGold = playerGold;
        this.ownerTile = ownerTile;
        //y좌표가 낮을수록 앞으로 나오게
        this.GetComponent<SpriteRenderer>().sortingOrder = -(int)this.transform.position.y + 10;
        // 무기 속성이 캐논, 레이저일 때
        if (weaponType == WeaponType.Gun || weaponType == WeaponType.Laser ||
            weaponType == WeaponType.Mortar || weaponType == WeaponType.Shotgun ||
            weaponType == WeaponType.Spear || weaponType == WeaponType.Explosion || weaponType == WeaponType.Strawberry)
        {
            // 최초 상태를 WeaponState.SearchTarget으로 설정
            ChangeState(WeaponState.SearchTarget);
        }
        for (int i = 0; i < LevelUp.Length; i++)
            LevelUp[i].SetActive(false);
    }

    public void ChangeState(WeaponState newState)
    {
        // 이전에 재생중이던 상태 종료
        //Debug.Log(weaponState.ToString());
        StopCoroutine(weaponState.ToString());
        // 상태 변경
        weaponState = newState;

        //Debug.Log(weaponState.ToString());
        // 새로운 상태 재생
        StartCoroutine(weaponState.ToString());
    }


    private void RotateToTarget()
    {
        // 원점으로부터의 거리와 수평축으로부터의 각도를 이용해 위치를 구하는 극 좌표계 이용
        // 각도 = arctan(y/x)
        // x, y 변위값 구하기
        float dx = attackTarget.position.x - transform.position.x;
        float dy = attackTarget.position.y - transform.position.y;
        // x, y 변위값을 바탕으로 각도 구하기
        // 각도가 radian 단위이기 때문에 Mathf.Rad2Deg를 곱해 도 단위를 구함
        float degree = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, degree);
    }

    private IEnumerator SearchTarget()
    {
        while (true)
        {
            // 현재 타워에 가장 가까이 있는 공격 대상(적) 탐색
            attackTarget = FindClosestAttackTarget();
            if (attackTarget != null && attackTarget.gameObject.activeSelf)
            {
                if (weaponType == WeaponType.Gun)
                {
                    ChangeState(WeaponState.TryAttackGun);
                }
                else if (weaponType == WeaponType.Laser)
                {
                    ChangeState(WeaponState.TryAttackLaser);
                }
                else if (weaponType == WeaponType.Mortar)
                {
                    ChangeState(WeaponState.TryAttackMortar);
                }
                else if (weaponType == WeaponType.Shotgun)
                {
                    ChangeState(WeaponState.TryAttackShotgun);
                }
                else if (weaponType == WeaponType.Spear)
                {
                    ChangeState(WeaponState.TryAttackSpaer);
                }
                else if (weaponType == WeaponType.Explosion)
                {
                    ChangeState(WeaponState.TryAttackExplosion);
                }
                else if (weaponType == WeaponType.Strawberry)
                {
                    ChangeState(WeaponState.TryAttackStrawberry);
                }
            }
            yield return null;
        }
    }

    private IEnumerator TryAttackGun()
    {
        while (true)
        {
            // target을 공격하는게 가능한지 검사
            if (IsPossibleToAttackTarget() == false)
            {
                ChangeState(WeaponState.SearchTarget);
                break;
            }
            // 캐논 공격 (발사체 생성)
            SpawnProjectile();
            // attackRate 시간만큼 대기
            yield return new WaitForSeconds(towerTemplate.weapon[level].rate);

        }
    }

    private IEnumerator TryAttackLaser()
    {
        // 레이저, 레이저 타격 효과 활성화
        EnableLaser();

        while (true)
        {
            // target을 공격하는게 가능한지 검사
            if (IsPossibleToAttackTarget() == false)
            {
                // 레이저, 레이저 타격 효과 비활성화
                DisableLaser();
                ChangeState(WeaponState.SearchTarget);
                break;
            }

            // 레이저 공격
            SpawnLaser();

            yield return null;
        }
    }

    private IEnumerator TryAttackMortar()
    {
        while (true)
        {
            // target을 공격하는게 가능한지 검사
            if (IsPossibleToAttackTarget() == false)
            {
                ChangeState(WeaponState.SearchTarget);
                break;
            }
            // 박격포 공격 (발사체 생성)
            if (attackTarget != null)
            {
                SpawnMortarProjectile();
            }
            // attackRate 시간만큼 대기
            yield return new WaitForSeconds(towerTemplate.weapon[level].rate);

        }
    }

    // 샷건 타워 공격
    private IEnumerator TryAttackShotgun()
    {
        while (true)
        {
            // target을 공격하는게 가능한지 검사
            if (IsPossibleToAttackTarget() == false)
            {
                ChangeState(WeaponState.SearchTarget);
                break;
            }
            // 샷건 공격 (발사체 생성)
            SpawnProjectile_Multiple();
            // attackRate 시간만큼 대기
            yield return new WaitForSeconds(towerTemplate.weapon[level].rate);
        }
    }

    // 관통 타워 공격
    private IEnumerator TryAttackSpaer()
    {
        while (true)
        {
            // target을 공격하는게 가능한지 검사
            if (IsPossibleToAttackTarget() == false)
            {
                ChangeState(WeaponState.SearchTarget);
                break;
            }
            // 관통 공격 (발사체 생성)
            SpawnProjectile_Spear();
            // attackRate 시간만큼 대기
            yield return new WaitForSeconds(towerTemplate.weapon[level].rate);
        }
    }

    // 폭발 타워 공격
    private IEnumerator TryAttackExplosion()
    {
        while (true)
        {
            // target을 공격하는게 가능한지 검사
            if (IsPossibleToAttackTarget() == false)
            {
                ChangeState(WeaponState.SearchTarget);
                break;
            }
            // 관통 공격 (발사체 생성)
            SpawnProjectile_Explosion();
            // attackRate 시간만큼 대기
            yield return new WaitForSeconds(towerTemplate.weapon[level].rate);
        }
    }

    private IEnumerator TryAttackStrawberry()
    {
        while (true)
        {
            // target을 공격하는게 가능한지 검사
            if (IsPossibleToAttackTarget() == false)
            {
                ChangeState(WeaponState.SearchTarget);
                break;
            }
            // 관통 공격 (발사체 생성)
            SpawnProjectile_Strawberry();
            // attackRate 시간만큼 대기
            yield return new WaitForSeconds(towerTemplate.weapon[level].rate);
        }
    }

    public void OnBuffAroundTower()
    {
        // 현재 맵에 배치된 "Tower" 태그를 가진 모든 오브젝트 탐색
        GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower");

        for (int i = 0; i < towers.Length; ++i)
        {
            TowerWeapon weapon = towers[i].GetComponent<TowerWeapon>();

            // 이미 버프를 받고 있고, 현재 버프 타워의 레벨보다 높은 버프이면 패스
            if (weapon.BuffLevel > Level)
            {
                continue;
            }

            // 현재 버프 타워와 다른 타워의 거리를 검사해서 범위 안에 타워가 있으면
            if (Vector3.Distance(weapon.transform.position, transform.position) <= towerTemplate.weapon[level].range)
            {
                // 공격이 가능한 캐논, 레이저 타워이면
                if (weapon.WeaponType == WeaponType.Gun || weapon.WeaponType == WeaponType.Laser ||
                    weapon.WeaponType == WeaponType.Explosion || weapon.WeaponType == WeaponType.Mortar ||
                    weapon.WeaponType == WeaponType.Shotgun || weapon.WeaponType == WeaponType.Spear)
                {
                    // 버프에 의해 공격력 증가
                    weapon.AddedDamage = weapon.Damage * (towerTemplate.weapon[level].buff);
                    // 타워가 받고 있는 버프 레벨 설정
                    weapon.BuffLevel = Level;
                    weapon.buffTower = this;
                }
            }
        }
    }

    private Transform FindClosestAttackTarget()
    {
        // 제일 가까이 있는 적을 찾기 위해 최초 거리를 최대한 크게 설정
        // float closestDistSqr = Mathf.Infinity;
        // EnemySpawner의 EnemyList에 있는 현재 맵에 존재하는 모든 적 검사
        //Debug.Log(enemySpawner.EnemyList.Count);
        for (int i = 0; i < enemySpawner.EnemyList.Count; ++i)
        {
            float distance = Vector3.Distance(enemySpawner.EnemyList[i].transform.position, transform.position);
            // 현재 검사중인 적과의 거리가 공격범위 내에 있고, 현재까지 검사한 적보다 거리가 가까우면
            if (distance <= towerTemplate.weapon[level].range)// && distance <= closestDistSqr)
            {
                // closestDistSqr = distance;
                attackTarget = enemySpawner.EnemyList[i].transform;
                break;
            }
        }


        return attackTarget;
    }

    private bool IsPossibleToAttackTarget()
    {
        // target이 있는지 검사 (다른 발사체에 의해 제거, Goal 지점까지 이동해 삭제 등)
        if (attackTarget == null || !attackTarget.gameObject.activeSelf)
        {
            return false;
        }
        // target이 공격 범위 안에 있는지 검사 (공격 범위를 벗어나면 새로운 적 탐색)
        float distance = Vector3.Distance(attackTarget.position, transform.position);
        if (distance > towerTemplate.weapon[level].range || !attackTarget.gameObject.activeSelf)
        {
            attackTarget = null;
            return false;
        }
        return true;
    }



    private void SpawnProjectile()
    {
        if (attackTarget != null)
        {
            GameObject clone;                                                                  // 오브젝트 Pool에서 Dequeue해서 가져옴, 0번 : coco
            if (!ObjectPool.instance.objectPoolList[0].TryDequeue(out clone))
            {
                ObjectPool.instance.InsertQueue(0);
                clone = ObjectPool.instance.objectPoolList[0].Dequeue();
            }
            clone.transform.position = spawnPoint.position;                                   // Dequeue해서 가져온 Projectile의 position을 SpawnPoint로 바꾸어 줌
            clone.GetComponent<SpriteRenderer>().sprite = ProjectileSprite;
            // 생성된 발사체에게 공격대상(attackTarget) 정보 제공   
            // 공격력 = 타워 기본 공격력 + 버프에 의해 추가된 공격력
            float damage = towerTemplate.weapon[level].damage + AddedDamage;
            if (IsPossibleToAttackTarget())
                clone.GetComponent<Projectile>().Setup(attackTarget, damage);
            else
                clone.GetComponent<Projectile>().Setup(FindClosestAttackTarget(), damage);
        }
    }

    private void SpawnProjectile_Multiple()
    {
        if (level < 3) // 타워 레벨이 3보다 작은 경우(특수 능력X) 공격
        {
            if (attackTarget != null)
            {
                GameObject clone1;                                                                   // 오브젝트 Pool에서 Dequeue해서 가져옴, 1번 : jelly
                GameObject clone2;
                GameObject clone3;

                if (!ObjectPool.instance.objectPoolList[1].TryDequeue(out clone1))
                {
                    ObjectPool.instance.InsertQueue(1);
                    clone1 = ObjectPool.instance.objectPoolList[1].Dequeue();
                }
                if (!ObjectPool.instance.objectPoolList[1].TryDequeue(out clone2))
                {
                    ObjectPool.instance.InsertQueue(1);
                    clone2 = ObjectPool.instance.objectPoolList[1].Dequeue();
                }
                if (!ObjectPool.instance.objectPoolList[1].TryDequeue(out clone3))
                {
                    ObjectPool.instance.InsertQueue(1);
                    clone3 = ObjectPool.instance.objectPoolList[1].Dequeue();
                }

                clone1.transform.position = spawnPoint.position;
                clone2.transform.position = spawnPoint.position;
                clone3.transform.position = spawnPoint.position;

                clone1.GetComponent<SpriteRenderer>().sprite = ProjectileSprite;
                clone2.GetComponent<SpriteRenderer>().sprite = ProjectileSprite;
                clone3.GetComponent<SpriteRenderer>().sprite = ProjectileSprite;
                // 생성된 발사체에게 공격대상(attackTarget) 정보 제공
                // 공격력 = 타워 기본 공격력 + 버프에 의해 추가된 공격력
                float damage = towerTemplate.weapon[level].damage + AddedDamage;
                // 세 갈래로 나누어지는 공격을 위해 Vector3.left, right를 더해줌
                if (IsPossibleToAttackTarget())
                {
                    Vector3 targetPos = attackTarget.position;
                    clone1.GetComponent<Projectile_Multiple>().Setup(targetPos, damage, -1);
                    clone2.GetComponent<Projectile_Multiple>().Setup(targetPos, damage);
                    clone3.GetComponent<Projectile_Multiple>().Setup(targetPos, damage, 1);
                }
                else
                {
                    Debug.Log("Find");
                    Vector3 targetPos = FindClosestAttackTarget().position;
                    clone1.GetComponent<Projectile_Multiple>().Setup(targetPos, damage, -1);
                    clone2.GetComponent<Projectile_Multiple>().Setup(targetPos, damage);
                    clone3.GetComponent<Projectile_Multiple>().Setup(targetPos, damage, 1);
                }
            }
        }
        else if(level == 3) // 타워 레벨이 3보다 크고, 어떤 분기를 선택했는지에 따라 공격이 달라짐
        {
            if (attackTarget != null)
            {
                GameObject clone1;                                                                   // 오브젝트 Pool에서 Dequeue해서 가져옴, 1번 : jelly
                GameObject clone2;
                GameObject clone3;

                if (!ObjectPool.instance.objectPoolList[1].TryDequeue(out clone1))
                {
                    ObjectPool.instance.InsertQueue(1);
                    clone1 = ObjectPool.instance.objectPoolList[1].Dequeue();
                }
                if (!ObjectPool.instance.objectPoolList[1].TryDequeue(out clone2))
                {
                    ObjectPool.instance.InsertQueue(1);
                    clone2 = ObjectPool.instance.objectPoolList[1].Dequeue();
                }
                if (!ObjectPool.instance.objectPoolList[1].TryDequeue(out clone3))
                {
                    ObjectPool.instance.InsertQueue(1);
                    clone3 = ObjectPool.instance.objectPoolList[1].Dequeue();
                }

                clone1.transform.position = spawnPoint.position;
                clone2.transform.position = spawnPoint.position;
                clone3.transform.position = spawnPoint.position;

                clone1.GetComponent<SpriteRenderer>().sprite = towerTemplate.weapon[level].projectileSprites[0];
                clone2.GetComponent<SpriteRenderer>().sprite = towerTemplate.weapon[level].projectileSprites[1];
                clone3.GetComponent<SpriteRenderer>().sprite = towerTemplate.weapon[level].projectileSprites[2];
                // 생성된 발사체에게 공격대상(attackTarget) 정보 제공
                // 공격력 = 타워 기본 공격력 + 버프에 의해 추가된 공격력
                float damage = towerTemplate.weapon[level].damage + AddedDamage;
                // 세 갈래로 나누어지는 공격을 위해 Vector3.left, right를 더해줌
                if (IsPossibleToAttackTarget())
                {
                    Vector3 targetPos = attackTarget.position;
                    clone1.GetComponent<Projectile_Multi_IGNDEF>().Setup(targetPos, damage, -1);
                    clone2.GetComponent<Projectile_Multi_IGNDEF>().Setup(targetPos, damage);
                    clone3.GetComponent<Projectile_Multi_IGNDEF>().Setup(targetPos, damage, 1);
                }
                else
                {
                    Debug.Log("Find");
                    Vector3 targetPos = FindClosestAttackTarget().position;
                    clone1.GetComponent<Projectile_Multi_IGNDEF>().Setup(targetPos, damage, -1);
                    clone2.GetComponent<Projectile_Multi_IGNDEF>().Setup(targetPos, damage);
                    clone3.GetComponent<Projectile_Multi_IGNDEF>().Setup(targetPos, damage, 1);
                }
            }
        }
        else
        {
            if (attackTarget != null)
            {
                GameObject[] clone = new GameObject[8];                             // 오브젝트 Pool에서 Dequeue해서 가져옴, 1번 : jelly

                for (int i = 0; i < 8; i++)
                {
                    if (!ObjectPool.instance.objectPoolList[1].TryDequeue(out clone[i]))
                    {
                        ObjectPool.instance.InsertQueue(1);
                        clone[i] = ObjectPool.instance.objectPoolList[1].Dequeue();
                    }
                }

                for(int i = 0; i < 8; i++)
                {
                    clone[i].transform.position = spawnPoint.position;
                    clone[i].GetComponent<SpriteRenderer>().sprite = ProjectileSprite;
                }

                // 생성된 발사체에게 공격대상(attackTarget) 정보 제공
                // 공격력 = 타워 기본 공격력 + 버프에 의해 추가된 공격력
                float damage = towerTemplate.weapon[level].damage + AddedDamage;
                // 세 갈래로 나누어지는 공격을 위해 Vector3.left, right를 더해줌
                if (IsPossibleToAttackTarget())
                {
                    Vector3 targetPos = attackTarget.position;
                    for(int i = 0; i < 8; i++)
                    {
                        clone[i].GetComponent<Projectile_Multiple>().Setup(damage, i);
                    }
                }
            }
        }
    }

    // 占쏙옙占쏙옙 占싼억옙 占쏙옙占쏙옙
    private void SpawnProjectile_Explosion()
    {
        //Debug.Log("占쌩삼옙");
        if (attackTarget != null)
        {
            GameObject clone;                                                                    // 오브젝트 Pool에서 Dequeue해서 가져옴, 2번 : r
            if (!ObjectPool.instance.objectPoolList[2].TryDequeue(out clone))
            {
                ObjectPool.instance.InsertQueue(2);
                clone = ObjectPool.instance.objectPoolList[2].Dequeue();
            }
            clone.transform.position = spawnPoint.position;                                     // Dequeue해서 가져온 Projectile의 position을 SpawnPoint로 바꾸어 줌
            clone.GetComponent<SpriteRenderer>().sprite = ProjectileSprite;
            // 생성된 발사체에게 공격대상(attackTarget) 정보 제공
            // 공격력 = 타워 기본 공격력 + 버프에 의해 추가된 공격력
            float damage = towerTemplate.weapon[level].damage + AddedDamage;
            if (IsPossibleToAttackTarget())
                clone.GetComponent<Projectile_Explosion>().Setup(attackTarget, damage, Range, ExplosionRange);
            else
                clone.GetComponent<Projectile_Explosion>().Setup(FindClosestAttackTarget(), damage, Range, ExplosionRange);
        }
    }

    // 占쌘곤옙占쏙옙 占싼억옙 占쏙옙占쏙옙
    private void SpawnMortarProjectile()
    {
        GameObject clone;                                                                    // 오브젝트 Pool에서 Dequeue해서 가져옴, 3번 : milk
        if (!ObjectPool.instance.objectPoolList[3].TryDequeue(out clone))
        {
            ObjectPool.instance.InsertQueue(3);
            clone = ObjectPool.instance.objectPoolList[3].Dequeue();
        }
        clone.transform.position = spawnPoint.position;                                     // Dequeue해서 가져온 Projectile의 position을 SpawnPoint로 바꾸어 줌
        clone.GetComponent<SpriteRenderer>().sprite = ProjectileSprite;
        // 생성된 발사체에게 공격대상(attackTarget) 정보 제공
        // 공격력 = 타워 기본 공격력 + 버프에 의해 추가된 공격력
        float damage = towerTemplate.weapon[level].damage + AddedDamage;
        if (IsPossibleToAttackTarget())
            clone.GetComponent<ProjectileMortar>().Setup(attackTarget, damage, enemySpawner, ExplosionRange);
        else
            clone.GetComponent<ProjectileMortar>().Setup(FindClosestAttackTarget(), damage, enemySpawner, ExplosionRange);
    }

    private void SpawnProjectile_Strawberry()
    {
        GameObject clone;                                                                    // 오브젝트 Pool에서 Dequeue해서 가져옴, 4번 : strawberry
        if (!ObjectPool.instance.objectPoolList[4].TryDequeue(out clone))
        {
            ObjectPool.instance.InsertQueue(4);
            clone = ObjectPool.instance.objectPoolList[4].Dequeue();
        }
        clone.transform.position = spawnPoint.position;                                     // Dequeue해서 가져온 Projectile의 position을 SpawnPoint로 바꾸어 줌
        clone.GetComponent<SpriteRenderer>().sprite = ProjectileSprite;
        // 생성된 발사체에게 공격대상(attackTarget) 정보 제공
        // 공격력 = 타워 기본 공격력 + 버프에 의해 추가된 공격력
        float damage = towerTemplate.weapon[level].damage + AddedDamage;
        if (IsPossibleToAttackTarget())
            clone.GetComponent<ProjectileStrawberry>().Setup(attackTarget, damage, enemySpawner, ExplosionRange);
        else
            clone.GetComponent<ProjectileStrawberry>().Setup(FindClosestAttackTarget(), damage, enemySpawner, ExplosionRange);
    }

    private void SpawnProjectile_Spear()
    {
        if (attackTarget != null)
        {
            GameObject clone;                                                                    // 오브젝트 Pool에서 Dequeue해서 가져옴, 5번 : spear
            if (!ObjectPool.instance.objectPoolList[5].TryDequeue(out clone))
            {
                ObjectPool.instance.InsertQueue(5);
                clone = ObjectPool.instance.objectPoolList[5].Dequeue();
            }
            clone.transform.position = spawnPoint.position;                                     // Dequeue해서 가져온 Projectile의 position을 SpawnPoint로 바꾸어 줌

            if (clone.GetComponent<SpriteRenderer>() != null)
                clone.GetComponent<SpriteRenderer>().sprite = ProjectileSprite;
            // 생성된 발사체에게 공격대상(attackTarget) 정보 제공
            // 공격력 = 타워 기본 공격력 + 버프에 의해 추가된 공격력
            float damage = towerTemplate.weapon[level].damage + AddedDamage;
            if (IsPossibleToAttackTarget())
                clone.GetComponent<Projectile_Spear>().Setup(attackTarget, damage, Range);
            else
                clone.GetComponent<Projectile_Spear>().Setup(FindClosestAttackTarget(), damage, Range);
        }
    }

    private void EnableLaser()
    {
        lineRenderer.gameObject.SetActive(true);
        hitEffect.gameObject.SetActive(true);
    }

    private void DisableLaser()
    {
        lineRenderer.gameObject.SetActive(false);
        hitEffect.gameObject.SetActive(false);
    }

    private void SpawnLaser()
    {
        Vector3 direction = attackTarget.position - spawnPoint.position;
        RaycastHit2D[] hit = Physics2D.RaycastAll(spawnPoint.position, direction, towerTemplate.weapon[level].range, targetLayer);
        
        // 같은 방향으로 여러 의 광선을 쏴서 그 중 현재 attackTarget과 동개일한 오브젝트를 검출
        for (int i = 0; i < hit.Length; ++i)
        {
            if (hit[i].transform == attackTarget)
            {
                // 선의 시작지점
                lineRenderer.SetPosition(0, spawnPoint.position);
                // 선의 목표지점
                lineRenderer.SetPosition(1, new Vector3(hit[i].point.x, hit[i].point.y, 0) + Vector3.back);
                // 타격 효과 위치 설정
                hitEffect.position = hit[i].point;
                // 적 체력 감소 (1초에 damage만큼 감소)
                // 공격력 = 타워 기본 공격력 + 버프에 의해 추가된 공격력
                float damage = towerTemplate.weapon[level].damage + AddedDamage;

                // 레이저 타워 레벨이 4면 시간이 지속되면 공격력 증가
                if(level == 4)
                {
                    //attackTarget.GetComponent<EnemyHP>().TakeLaserDamage(damage * time);
                }
                // 레벨 5 적 이동속도 감소
                else if(level == 5)
                {
                    attackTarget.GetComponent<Enemy>().SetMoveSpeed(attackTarget.GetComponent<Enemy>().GetMoveSpeed() / 2);
                }
                else
                {
                    attackTarget.GetComponent<EnemyHP>().TakeLaserDamage(damage * Time.deltaTime);
                }
            }
        }
        //attackTarget.GetComponent<Enemy>().SetMoveSpeed(enemy_speed);
    }


    public bool Upgrade_1()
    {
        // 타워 설치 사운드 재생
        SoundManager.instance.SFXPlay("TowerUpgrade", upgradeClip);
        // 타워 업그레이드에 필요한 골드가 충분한지 검사
        if (playerGold.CurrentGold < towerTemplate.weapon[level + 1].cost)
        {
            return false;
        }

        // 타워 레벨 증가
        level++;
        // 타워 외형 변경 (Sprite)
        spriteRenderer.sprite = towerTemplate.weapon[level].sprite;
        // 골드 차감
        playerGold.CurrentGold -= towerTemplate.weapon[level].cost;
        LevelUp[level - 1].SetActive(true);

        // 무기 속성이 레이저이면
        if (weaponType == WeaponType.Laser)
        {
            // 레벨에 따라 레이저의 굵기 설정
            lineRenderer.startWidth = 0.05f + level * 0.05f;
            lineRenderer.endWidth = 0.05f;
        }

        // 타워가 업그레이드 될 때 모든 버프 타워의 버프 효과 갱신
        // 현재 타워가 버프 타워인 경우, 현재 타워가 공격 타워인 경우
        towerSpawner.OnBuffAllBuffTowers();

        return true;
    }


    public bool Upgrade_2()
    {
        // 타워 설치 사운드 재생
        SoundManager.instance.SFXPlay("TowerUpgrade", upgradeClip);
        // 타워 업그레이드에 필요한 골드가 충분한지 검사
        if (playerGold.CurrentGold < towerTemplate.weapon[level + 2].cost)
        {
            return false;
        }

        // 타워 레벨 증가
        level += 2;
        LevelUp[level - 2].SetActive(true);
        // 타워 외형 변경 (Sprite)
        spriteRenderer.sprite = towerTemplate.weapon[level].sprite;
        // 골드 차감
        playerGold.CurrentGold -= towerTemplate.weapon[level].cost;

        // 무기 속성이 레이저이면
        if (weaponType == WeaponType.Laser)
        {
            // 레벨에 따라 레이저의 굵기 설정
            lineRenderer.startWidth = 0.05f + (level) * 0.05f;
            lineRenderer.endWidth = 0.05f;
        }

        // 타워가 업그레이드 될 때 모든 버프 타워의 버프 효과 갱신
        // 현재 타워가 버프 타워인 경우, 현재 타워가 공격 타워인 경우
        towerSpawner.OnBuffAllBuffTowers();

        return true;
    }
    public void Sell()
    {
        // 타워 판매 사운드 재생
        SoundManager.instance.SFXPlay("TowerSell", sellClip);
        // 골드 증가
        playerGold.CurrentGold += towerTemplate.weapon[level].sell;
        // 현재 타일에 다시 타워 건설이 가능하도록 설정
        ownerTile.IsBuildTower = false;

        // 현재 맵에 배치된 "Tower" 태그를 가진 모든 오브젝트 탐색
        GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower");

        for (int i = 0; i < towers.Length; ++i)
        {
            towers[i].GetComponent<TowerWeapon>().BuffLevel = 0;
            towers[i].GetComponent<TowerWeapon>().AddedDamage = 0;
            //Debug.Log(towers[i].GetComponent<TowerWeapon>().AddedDamage);
        }
        towerSpawner.OnBuffAllBuffTowers();

        // 판매 이펙트 보여주기
        towerSpawner.sellEffect.SetActive(true);
        towerSpawner.sellEffect.GetComponent<TowerBuildEffect>().Boom();
        towerSpawner.sellEffect.transform.position = transform.position + Vector3.down / 2;

        // 타워 파괴
        Destroy(gameObject);
    }
}

/*
 * File : TowerWeapon.cs
 * Desc
 *	: 적을 공격하는 타워 무기
 *	
 * Functions
 *	: ChangeState() - 코루틴을 이용한 FSM에서 상태 변경 함수
 *	: RotateToTarget() - target 방향으로 o
 *	: SearchTarget() - 현재 타워에 가장 근접한 적 탐색
 *	: TryAttackGun() - target으로 설정된 대상에게 캐논 공격
 *	: TryAttackLaser() - target으로 설정된 대상에게 레이저 공격
 *	: FindClosestAttackTarget() - 현재 타워에 가장 근접한 공격 대상(적) 탐색
 *	: IsPossibleToAttackTarget() - AttackTarget이 있는지, 공격 가능한지 검사
 *	: SpawnProjectile() - 캐논 발사체 생성
 *	: EnableLaser() - 레이저, 레이저 타격 효과 활성화
 *	: DisableLaser() - 레이저, 레이저 타격 효과 비활성화
 *	: SpawnLaser() - 레이저 공격, 레이저 타격 효과, 적 체력 감소
 *	: Upgrade() - 타워 업그레이드
 *	: Sell() - 타워 판매
 *	
 */