using UnityEngine;
using UnityEngine.UI;
using MintLandDemo.Core.Event;
using MintLandDemo.Core.Game;

namespace MintLandDemo.UI.Character
{
    /// <summary>
    /// 玩家血条 UI。订阅 PlayerDamagedEvent 刷新，装备变化时也刷新上限。
    /// 使用 Unity 内置 UGUI Text（不是 TextMeshPro）。
    /// </summary>
    public class PlayerHpUI : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Text hpText;

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Subscribe<EquipmentChangedEvent>(OnEquipmentChanged);
            Refresh();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Unsubscribe<EquipmentChangedEvent>(OnEquipmentChanged);
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt) => Refresh();
        private void OnEquipmentChanged(EquipmentChangedEvent evt) => Refresh();

        public void Refresh()
        {
            var player = GameRoot.Instance?.Context?.Data?.Player;
            if (player == null) return;

            int maxHp = player.finalMaxHp > 0 ? player.finalMaxHp : player.baseMaxHp;
            int curHp = Mathf.Clamp(player.currentHp, 0, maxHp);

            if (fillImage != null)
                fillImage.fillAmount = maxHp > 0 ? (float)curHp / maxHp : 0f;

            if (hpText != null)
                hpText.text = $"{curHp} / {maxHp}";
        }
    }
}