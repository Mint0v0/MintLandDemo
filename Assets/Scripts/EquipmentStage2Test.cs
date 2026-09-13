using UnityEngine;
using UnityEngine.InputSystem;
using MintLandDemo.Core.Game;
using MintLandDemo.Gameplay.Equipment;
using MintLandDemo.Gameplay.Inventory;

public class EquipmentStage2Test : MonoBehaviour {
    private EquipmentSystem _equip;
    private void Start() { _equip = FindObjectOfType<EquipmentSystem>(); }

    private void Update() {
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame) RunTest();
    }

    private void RunTest() {
        GameRuntimeData data = GameRoot.Instance?.Context?.Data;
        int baseAttack = data.Player.baseAttack;

        InventorySystem.AddItem("Axe", 1);      // �ȸ������ڶ�������
        _equip.EquipWeapon(1, "Axe");           // װ����λ 1
        Debug.Log($"[Test] ˫���� finalAttack={data.Player.finalAttack}��Ӧ=����+��+����");

        _equip.UnequipWeapon(0);                // ж�²�λ 0�����黹����
        Debug.Log($"[Test] ж��0�� finalAttack={data.Player.finalAttack}��Ӧ=����+����");

        _equip.SwitchActiveWeapon(1);                 // V1 �л�����־ȷ�ϣ�
        Debug.Log($"[Test] װ��������={_equip.GetEquippedWeapons().Count}");
    }
}
