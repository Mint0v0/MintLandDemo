using UnityEngine;

namespace MintLandDemo.Core.Event
{
    public struct EnemyKilledEvent
    {
        public string enemyId;
        public Vector3 position;
    }

    public struct PlayerDamagedEvent
    {
        public int Damage;
        public int CurrentHp;
        public string AttackerId;
    }

    public struct PlayerDiedEvent
    {
        public string KillerId;
    }

    /// <summary>
    /// 锁定目标变化事件。Target == null 表示解除锁定。
    /// 由 LockOnSystem 发布，UI / 相机 / 玩家控制器订阅。
    /// </summary>
    public struct LockOnChangedEvent
    {
        public Transform Target;
        public bool HasTarget => Target != null;
    }
}