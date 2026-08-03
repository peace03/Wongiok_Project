using UnityEngine;

public class GameInputReader : MonoBehaviour
{
    private GameInputAction _input;

    #region Player Input
    public Vector2 MoveInput => _input.Player.Move.ReadValue<Vector2>();
    public bool JumpTriggered => _input.Player.Jump.WasPressedThisFrame();
    public bool IsJumping => _input.Player.Jump.IsPressed();
    public bool AttackTriggered => _input.Player.Attack.WasPressedThisFrame();
    public bool DashTriggered => _input.Player.Dash.WasPressedThisFrame();
    public bool ParryTriggered => _input.Player.Parry.WasPressedThisFrame();
    public bool UseHealItemTriggered => _input.Player.UseHealItem.WasPressedThisFrame();
    public bool UseParry => _input.Player.UseParry.WasPressedThisFrame();
    #endregion

    #region UI Input
    public bool MenuPressed => _input.UI.ToggleMenu.WasPressedThisFrame();
    public bool TitleStartPressed => _input.UI.StartTitle.WasPressedThisFrame();
    public bool PreviousPauseTabPressed => _input.UI.PreviousPauseTab.WasPressedThisFrame();
    public bool NextPauseTabPressed => _input.UI.NextPauseTab.WasPressedThisFrame();
    public bool SubmitPressed => _input.UI.Submit.WasPressedThisFrame();
    public Vector2 UINavigationInput => _input.UI.Navigate.ReadValue<Vector2>();
    #endregion

    #region Test Input
    public bool TestF1Pressed => _input.Test.F1.WasPressedThisFrame();
    public bool TestF2Pressed => _input.Test.F2.WasPressedThisFrame();
    public bool TestF3Pressed => _input.Test.F3.WasPressedThisFrame();
    public bool TestF4Pressed => _input.Test.F4.WasPressedThisFrame();
    public bool TestF5Pressed => _input.Test.F5.WasPressedThisFrame();
    public bool TestF6Pressed => _input.Test.F6.WasPressedThisFrame();
    public bool TestF7Pressed => _input.Test.F7.WasPressedThisFrame();
    public bool TestF8Pressed => _input.Test.F8.WasPressedThisFrame();
    public bool TestF9Pressed => _input.Test.F9.WasPressedThisFrame();
    public bool TestF10Pressed => _input.Test.F10.WasPressedThisFrame();
    public bool TestF11Pressed => _input.Test.F11.WasPressedThisFrame();
    #endregion

    #region Skill Input
    public bool SkillAPressed => _input.Player.SkillA.WasPressedThisFrame();
    public bool SkillSPressed => _input.Player.SkillS.WasPressedThisFrame();
    public bool SkillDPressed => _input.Player.SkillD.WasPressedThisFrame();
    #endregion

    private void Awake()
    {
        _input = new GameInputAction();
    }

    private void OnEnable()
    {
        _input?.Enable();
    }

    private void OnDisable()
    {
        _input?.Disable();
    }

    private void OnDestroy()
    {
        _input?.Dispose();
    }

    public bool ReleaseSniperSkill(int skillIndex)
    {
        return skillIndex switch
        {
            (int)ACTIVE_SKILL_SLOT_TYPE.A   => _input.Player.SkillA.IsPressed(),
            (int)ACTIVE_SKILL_SLOT_TYPE.S   => _input.Player.SkillS.IsPressed(),
            (int)ACTIVE_SKILL_SLOT_TYPE.D   => _input.Player.SkillD.IsPressed(),
            _                               => false
        };
    }
}
